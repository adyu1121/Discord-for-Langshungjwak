using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;

using System.Collections.Generic;
using System.Threading;
using System.Net.Http;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using static Lang_shung_jwak.Database;

namespace Lang_shung_jwak;


class Program
{
    private Dictionary<Guid, Runner> interpreter;
    private DiscordSocketClient client;
    private InteractionService interactionService;
    private IServiceProvider services;
    private Timer timer;

    //인터프리터의 런타임 지속시간
    private const int lifeTime =
            1000 *   //1초
            60 *     //1분
            60 *     //1시간
            24 *     //하루
            3;       //일 지정(3일)

    #region Program
    public Program()
    {
        client = new DiscordSocketClient();
        interpreter = new Dictionary<Guid, Runner>();
    }
    async Task StartBot()
    {
        client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        });
        interactionService = new InteractionService(client);

        client.Log += LogHandle;
        client.Ready += ClientReady;
        client.InteractionCreated += InteractionHandle;
        client.MessageReceived += MessageHandle;

        string token = Environment.GetEnvironmentVariable("BOT_TOKEN");
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
    }
    private async Task ClientReady()
    {
        var services = new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton(interactionService)
            .BuildServiceProvider();

        this.services = services;
        await interactionService.AddModulesAsync(typeof(Program).Assembly, services);
        await interactionService.RegisterCommandsGloballyAsync();

        Database.Init();
        Log(Guid.Empty, "봇 시작");
        timer = new Timer((callback)=>Task.Run(RunnerClear), null, 0, lifeTime );
    }
    private Task LogHandle(LogMessage message)
    {
        Log(Guid.Empty, message.ToString());
        return Task.CompletedTask;
    }
    #endregion

    #region Handle
    private async Task MessageHandle(SocketMessage message)
    {
        if (message.Author.IsBot) return; // 봇 메시지는 무시
        if (message.Channel is not SocketGuildChannel) return; // 채널이 서버 채널인지 확인
    }
    private async Task InteractionHandle(SocketInteraction interaction)
    {
        if (interaction is SocketMessageComponent component) await MessageComponentHandle(component);
        else if (interaction is SocketModal modal)           await ModalHandle(modal);
        else if (interaction is SocketSlashCommand command)  await SlashCommandHandle(command, interaction);
    }
    private async Task MessageComponentHandle(SocketMessageComponent component)
    {
        var operater = SocketOperator.Parse(component.Data.CustomId);

        if (operater.OpCode == SocketOperator.Operator.InputRunner)
        {
            if (!Guid.TryParse(operater[0], out Guid id))
            {
                await component.RespondAsync($"GUID가 잘못되었습니다.", ephemeral: false);
                return;
            }

            if (interpreter.ContainsKey(id))
            {
                var modal = new ModalBuilder()
                    .WithTitle("질문에 답변하기")
                    .WithCustomId($"Input")
                    .AddTextInput("답변을 입력하세요:", $"{id}", TextInputStyle.Short, "여기에 입력");
                await component.RespondWithModalAsync(modal.Build());
            }
            else
            {
                await component.RespondAsync($"인터프리터({id})가 없습니다", ephemeral: false);
            }
        }
        else
        {
            await component.RespondAsync("유효하지 않은 응답");
        }
    }
    private async Task ModalHandle(SocketModal modal)
    {
        string id = modal.Data.CustomId;

        //인터프리터 중지
        if (id == "Delete")
        {
            var input = modal.Data.Components.First();

            if (!Guid.TryParse(input?.Value, out Guid guid))
            {
                Log(Guid.Empty, $"[메인 프로세스] Delete {guid} GUID가 잘못되었습니다");
                await modal.RespondAsync("GUID가 잘못되었습니다");
                return;
            }
            if (!interpreter.ContainsKey(guid) || !interpreter[guid].isRunning)//키가 존재하고 작동중일때만
            {
                Log(Guid.Empty, $"[메인 프로세스] Delete {guid} GUID가 존재하지 않습니다");
                await modal.RespondAsync("GUID가 존재하지 않습니다");
                return;
            }

            Log(Guid.Empty, $"[메인 프로세스] 인터프리터 {guid} 강제중지");
            interpreter[guid].Stop();
            await modal.RespondAsync($"{guid} 강제중지 완료");
        }

        //인터프리터 수명 늘림
        else if (id == "Long")
        {
            var input = modal.Data.Components.First();

            if (!Guid.TryParse(input?.Value, out Guid guid))
            {
                Log(Guid.Empty, $"[메인 프로세스] Long {guid} GUID가 잘못되었습니다");
                await modal.RespondAsync("GUID가 잘못되었습니다");
                return;
            }
            if (!interpreter.ContainsKey(guid) || !interpreter[guid].isRunning)//키가 존재하고 작동중일때만
            {
                Log(Guid.Empty, $"[메인 프로세스] Long {guid} GUID가 존재하지 않습니다");
                await modal.RespondAsync("GUID가 존재하지 않습니다");
                return;
            }

            Log(Guid.Empty, $"[메인 프로세스] 인터프리터 {guid} 의 기한을 늘렸습니다");
            interpreter[guid].DateTimeToNow();
            await modal.RespondAsync($"{guid} 의 기한을 늘렸습니다");
        }

        //인터프리터 생성
        else if (id == "Creat")
        {
            var input = modal.Data.Components.First();

            Log(Guid.Empty, "[메인 프로세스] 버튼으로 러너 생성");
            var result = CreateRunner(modal.Channel, input?.Value).Result;

            await modal.RespondAsync(result.msg, ephemeral:true);
            if (result.guid != Guid.Empty) interpreter[result.guid].Run();
        }
        //인터프리터 입력
        else if(id == "Input")
        {
            var input = modal.Data.Components.First();

            if (!Guid.TryParse(input.CustomId, out Guid guid))
            {
                Log(Guid.Empty, $"[메인 프로세스] Input {guid} GUID가 잘못되었습니다");
                await modal.RespondAsync("GUID가 아님");
                return;
            }
            if (!interpreter.ContainsKey(guid))
            {
                Log(Guid.Empty, $"[메인 프로세스] Input {guid} GUID가 존재하지 않습니다");
                await modal.RespondAsync("GUID가 없음");
                return;
            }

            try
            {
                Log(Guid.Empty, $"[메인 프로세스] Input {input.Value}");

                if (int.TryParse(input.Value, out int result))
                {
                    Log(Guid.Empty, $"[메인 프로세스] Input {guid} 입력 완료");
                    interpreter[guid].Input(result);
                    await modal.RespondAsync("입력 완료");
                }
                else
                {
                    Log(Guid.Empty, $"[메인 프로세스] Input {guid} 숫자만을 입력해주세요");
                    await modal.RespondAsync("숫자만을 입력해주세요");
                }
            }
            catch(InvalidOperationException ex)
            {
                Log(Guid.Empty, $"[메인 프로세스] Input {guid} 런타임이 끝났습니다.");
                await modal.RespondAsync("런타임이 끝났습니다.");
            }
            catch (Exception ex)
            {
                Log(Guid.Empty, $"[메인 프로세스] Input {guid} 실행 중 오류 : {ex.Message}");
                await modal.RespondAsync($"실행 중 오류 : {ex.Message}");
            }
        }

        else if (id == "Report")
        {
            var msg = modal.Data.Components.First(x => x.CustomId == "msg");
            var code = modal.Data.Components.First(x => x.CustomId == "code");

            bool result = Report(msg?.Value, code?.Value);
            await modal.RespondAsync(result ? "버그가 제보되었습니다" : "버그 제보에 실패했습니다. 나중에 다시 시도하세요");
        }
    }
    private async Task SlashCommandHandle(SocketSlashCommand command, SocketInteraction interaction)
    {
        await interactionService.ExecuteCommandAsync(new SocketInteractionContext(client, interaction), services);
    }
    #endregion

    #region Runner
    private async Task<(Guid guid, string msg)> CreateRunner(IMessageChannel channel, string code)
    {
        //빈 코드면 실행 X
        if (string.IsNullOrEmpty(code)) return (Guid.Empty, "code는 비어있을 수 없습니다");

        //Guid 생성
        Guid guid;
        do guid = Guid.NewGuid(); while (interpreter.ContainsKey(guid) && interpreter[guid].isRunning);

        //러너 실행
        var runner = new Runner(code, channel, guid, DateTime.Now);

        try
        {
            Log(Guid.Empty, $"[메인 프로세스] Runner 생성 시작 : {guid}");

            runner.Parsing();
            interpreter.Add(guid, runner);

            Log(Guid.Empty, $"[메인 프로세스] Runner 생성 성공 : {guid}");
            return (guid, $"생성 성공! : GUID({guid})");
        }
        catch (Exception ex)
        {
            Log(Guid.Empty, $"[메인 프로세스] 파싱 불가 : {ex.GetType()} : {ex.Message}");

            runner.Stop();
            return (Guid.Empty, $"실행 중 오류 발생 : {ex.Message}");
        }
    }
    private void RunnerClear()
    {
        Log(Guid.Empty, "[메인 프로세스] 인터프리터 정리 시작");

        foreach ((var key, var value) in interpreter)
        {
            if (!value.isRunning)
            {
                Log(Guid.Empty, $"[메인 프로세스] 인터프리터 {key} 삭제");
                interpreter.Remove(key);
            }
            else if((DateTime.Now - value.dateTime).TotalSeconds > lifeTime)
            {
                Log(Guid.Empty, $"[메인 프로세스] 인터프리터 {key} 시간초과");
                value.Stop();
                interpreter.Remove(key);
            }
        }

        Log(Guid.Empty, "[메인 프로세스] 인터프리터 정리 끝");
    }
    #endregion

    #region Entry
    static async Task Main(string[] args)
    {
        Program bot = new Program();
        await bot.StartBot();
        await Task.Delay(-1);
    }
    #endregion
}

