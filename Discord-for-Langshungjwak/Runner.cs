using Discord;
using Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using static Lang_shung_jwak.Database;

namespace Lang_shung_jwak;

//TODO : 입 출력 토큰 무시(에러 안뜸)
//TODO : 조건 없는 조건문이 에러남
public class Runner
{
    public bool isRunning { get; private set; }
    public DateTime dateTime;

    private BlockingCollection<int?> inputBuffer;
    private Dictionary<int, int> variables;

    private readonly string code;
    private readonly Guid guid;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly CancellationToken token;

    private IUserMessage console;
    private IMessageChannel channel;
    private string output;

    private List<Command> commands;
    public class RunnerException : Exception
    {
        public RunnerException(string message) : base(message) { }
    }
    private class Command
    {
        /// <summary>
        ///  0 : Set Var
        ///  1 : Write Int
        ///  2 : Write char
        ///  3 : Read
        ///  4 : Goto
        /// </summary>
        public int op;
        public int opValue;
        public ExpressionTree opExpression;

        public bool hasIf;
        public ExpressionTree ifExpression;

        public Command(ExpressionTree.ReadValue read)
        {
            op = -1;
            opExpression = new(read);
            ifExpression = new(read);
        }
    }

    public Runner(string code, IMessageChannel channel, Guid guid, DateTime dateTime)
    {
        if (string.IsNullOrEmpty(code)) throw new ArgumentNullException("code 는 Null이나 Empty일 수 없습니다.");
        Log(guid, $"[인터프리터 {guid}] 인스턴스 생성됨\n{code}");

        this.channel = channel;
        this.code = code;
        this.guid = guid;
        output = string.Empty;

        inputBuffer = new BlockingCollection<int?>();
        variables = new Dictionary<int, int>();
        commands = new List<Command>();
        cancellationTokenSource = new CancellationTokenSource();

        token = cancellationTokenSource.Token;

        this.isRunning = true;
        this.dateTime = dateTime;
    }

    public void Run()
    {
        Task.Run(RunInterpreter, cancellationTokenSource.Token);
    }
    public void Parsing()
    {
        //코드 파싱
        if (!code.StartsWith("교주님"))
            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
        string removingCommentsCode = Parser.SkipTokenRemove(code);

        using var sr = new StringReader(removingCommentsCode);

        while (sr.Peek() != -1)
        {
            Command command = new Command(GetVariables);
            var line = sr.ReadLine()!;

            //현재 연산식이 조건문인지
            bool isIfExpression = false;
            //첫번쨰 토큰인지
            bool isFirstToken = true;

            //TODO : 나중에 파서가 막아주는거 조건 빼기
            foreach (Parser.Token tok in Parser.Tokenize(line))
            {
                switch (tok.type)
                {
                    // 숫자 푸시
                    case 0:
                        if (isFirstToken)
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        else
                        {
                            if (isIfExpression) command.ifExpression.Add(0, (int)tok.value);
                            else command.opExpression.Add(0, (int)tok.value);
                        }
                        break;

                    // 연산 수행
                    case 1:
                        if (isFirstToken)
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        else
                        {
                            if (isIfExpression) command.ifExpression.Add(2, (int)tok.value);
                            else command.opExpression.Add(2, (int)tok.value);
                        }
                        break;

                    // 변수 접근
                    case 2:
                        //첫번째 토큰이면 대입문임
                        if (isFirstToken)
                        {
                            command.op = 0;
                            command.opValue = (int)tok.value;
                        }
                        //아니면 변수 값을 불러옴
                        else
                        {
                            if (isIfExpression) command.ifExpression.Add(1, (int)tok.value);
                            else command.opExpression.Add(1, (int)tok.value);
                        }
                        break;

                    //ASCII 출력
                    case 3:
                        if (isFirstToken)
                        {
                            command.op = 2;
                            command.opValue = (int)tok.value;
                        }
                        else
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        break;

                    //숫자 출력
                    case 4:
                        if (isFirstToken)
                        {
                            command.op = 1;
                            command.opValue = (int)tok.value;
                        }
                        else
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        break;

                    // 입력받기
                    case 5:
                        if (isFirstToken)
                        {
                            command.op = 3;
                            command.opValue = (int)tok.value;
                        }
                        else
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        break;

                    // IF 문 (조건 체크)
                    case 6:
                        isIfExpression = true;
                        command.hasIf = true;
                        break;

                    // GOTO 문
                    case 7:
                        if (isFirstToken)
                        {
                            command.op = 4;
                            command.opValue = (int)tok.value;
                        }
                        else
                        {
                            throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                        }
                        break;

                    //이상한 토큰
                    default:
                        throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                }
                isFirstToken = false;
            }
            commands.Add(command);
        }
    }
    private async void RunInterpreter()
    {
        //코드 실행
        try
        {
            Log(guid, $"[인터프리터 {guid}] 코드 실행 시작");

            int line = 0;

            while (line >= 0 && line < commands.Count)
            {
                var command = commands[line];
                Log(guid, $"[인터프리터 {guid}] {line}번쨰 커맨드 실행 시작");

                //만일 스레드가 종료되었다면 루프 빠져나오기
                if (token.IsCancellationRequested)
                {
                    Log(guid, $"[인터프리터 {guid}] 스레드 토큰이 만료되어 루프 종료");
                    break;
                }

                //만약 OP가 없다면 (빈 줄이거나 "교주님"토큰이라면) 스킵
                if (commands[line].op < 0)
                {
                    line++;
                    continue;
                }

                //만약 조건문이 존재한다면
                if (command.hasIf)
                {
                    //조건 검사
                    var value = command.ifExpression.GetValue();

                    //0이 아니라면 넘어가기
                    if (value != 0) 
                    {
                        line++;
                        continue;
                    }
                }
                
                //커맨드 실행
                switch (command.op)
                {
                    //Set Var
                    case 0:
                        int op = command.opValue;
                        int ex = command.opExpression.GetValue();
                        SetVariables(op, ex);
                        line++;
                        break;

                    //Write Number
                    case 1:
                        output += GetVariables(command.opValue).ToString();
                        line++;
                        break;

                    //Write ASCII
                    case 2:
                        output += (char)GetVariables(command.opValue);
                        line++;
                        break;

                    //Read
                    case 3:
                        await UpdateContent();
                        Log(guid, $"[인터프리터 {guid}] {line}번쨰 인풋 요청");
                        int? value = inputBuffer.Take();
                        if (value == null) continue;

                        SetVariables(command.opValue, value.Value);
                        line++;
                        break;

                    //GOTO
                    case 4:
                        line += command.opValue;
                        break;

                    //이상한 OP코드면 오류
                    default:
                        throw new RunnerException("유효하지 않은 OP코드");
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            // 컬렉션이 닫히면 루프가 종료됨
            Log(guid, $"[인터프리터 {guid}] 예외: 컬렉션이 닫힘\nMessage:{ex.Message}");
            output += $"\n런타임 중지됨";
            Stop();
        }
        catch (Exception ex)
        {
            Log(guid, $"[인터프리터 {guid}] 런타임 에러 : {ex.GetType()}:{ex.Message}");
            output += $"\n오류 : {ex.Message}";
            Stop();
        }
        await UpdateContent();
        output += $"\n프로그램 종료";
        End();
    }

    private int GetVariables(int i)
    {
        if (!variables.ContainsKey(i))
            variables.Add(i, 0);
        return variables[i];
    }
    private void SetVariables(int i, int v)
    {
        if (!variables.ContainsKey(i))
            variables.Add(i, 0);
        variables[i] = v;
    }
    private async Task UpdateContent()
    {
        var op = new SocketOperator(SocketOperator.Operator.InputRunner, guid.ToString());

        ButtonBuilder buttonBuilder = new ButtonBuilder()
            .WithDisabled(false)
            .WithLabel("입력하기")
            .WithCustomId(op.ToString())
            .WithStyle(ButtonStyle.Primary);

        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithButton(buttonBuilder);

        if (console == null) console = channel.SendMessageAsync("실행중...", components: componentBuilder.Build()).Result;
        await console.ModifyAsync(msg => msg.Content = string.IsNullOrEmpty(output) ? "실행중..." : output);
    }

    public void DateTimeToNow()
    {
        dateTime = DateTime.Now;
    }
    public void Input(int value)
    {
        Log(guid, $"[인터프리터 {guid}] 인풋 들어옴({value})");
        DateTimeToNow();
        inputBuffer.Add(value);
    }
    public void Stop()
    {
        Log(guid, $"[인터프리터 {guid}] 강제정지");
        //Task 종료
        cancellationTokenSource.Cancel();
        //블로킹된 버퍼 작동
        inputBuffer.Add(null);
    }
    public void End()
    {
        inputBuffer.CompleteAdding();
        isRunning = false;
        Log(guid, $"[인터프리터 {guid}] 동작 끝남(스레드 종료)");
    }

}