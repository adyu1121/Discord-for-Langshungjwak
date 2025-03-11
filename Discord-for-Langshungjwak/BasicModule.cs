using Discord.Interactions;
using Discord;
using Lang_shung_jwak;
using YHIUYIUL;
using System.Collections.Generic;
using System.Threading.Tasks;
public class BasicModule : InteractionModuleBase<SocketInteractionContext>
{
    #region Channel
    [SlashCommand("채널추가", "랭슝좍 인터프리터가 감지할 채널을 설정합니다")]
    public async Task AddChannel()
    {
        ulong channel = Context.Channel.Id;
        ulong guild = Context.Guild.Id;
        if (!Database.IsSetServer(guild))
        {
            Database.AddChannel(guild, channel);
            await RespondAsync("채널이 등록되었습니다");
        }
        else
        {
            await RespondAsync("채널이 이미 등록되어있습니다.");
        }
    }

    [SlashCommand("채널삭제", "랭슝좍 인터프리터가 감지할 채널을 제거합니다")]
    public async Task RemoveChannel()
    {
        ulong channel = Context.Channel.Id;
        ulong guild = Context.Guild.Id;

        if (Database.IsSetServer(guild) && Database.GetChannel(guild) == channel)
        {
            Database.RemoveChannel(guild);
            await RespondAsync("채널이 삭제되었습니다");
        }
        else
        {
            await RespondAsync("등록된 채널이 아닙니다");
        }
    }
    #endregion

    #region "Help"
    [SlashCommand("랭슝좍이란", "랭슝좍 언어에 대해서 설명합니다")]
    public async Task WhatIsLangshungjwak()
    {
        await RespondAsync(
            "# 랭슝좍이란?\n" +
            "랭슝좍은 트릭컬 리바이브의 리슝좍 밈에서 착안하여, 코드를 짜면서도 비비와 순수를 따잇하는 재미를 느끼기 위해 만들어진 언어입니다.\n" +
            "엄랭 Github에서 아이디어를 얻어 제작했습니다.\n" +
            "https://github.com/nabibear33/Lang-shung-jwak"
            , embed: infoembed.Build());
    }

    [SlashCommand("도움말", "도움말을 제공합니다.")]
    public async Task HelpCommand()
    {
        await RespondAsync(
            "이 봇은 아래 명령을 제공합니다. 또한, 채널을 등록하면 채널의 텍스트 또는 파일을 감지해 실행합니다.",
            embed: helpembed.Build());
    }
    #endregion

    #region Runner
    [SlashCommand("연결끊기", "랭슝좍 인터프리터를 제거합니다")]
    public async Task RemoveRunner()
    {
        // 모달 생성
        var modal = new ModalBuilder()
            .WithTitle("삭제할 인터프리터의 GUID를 입력해주세요")
            .WithCustomId("Delete")
            .AddTextInput("GUID", "Delete", TextInputStyle.Short, "여기에 입력");

        await RespondWithModalAsync(modal.Build());
    }

    [SlashCommand("기간늘림", "랭슝좍 인터프리터의 기한을 늘립니다")]
    public async Task LiveRunner()
    {
        var modal = new ModalBuilder()
            .WithTitle("삭제할 인터프리터의 GUID를 입력해주세요")
            .WithCustomId("Long")
            .AddTextInput("GUID", "Long", TextInputStyle.Short, "여기에 입력");

        await RespondWithModalAsync(modal.Build());
    }

    [SlashCommand("실행", "랭슝좍 코드를 실행합니다.")]
    public async Task RunLangshungjwak()
    {
        //var op = new SocketOperator(SocketOperator.Operator.CreatRunner);
        //ButtonBuilder buttonBuilder = new ButtonBuilder()
        //    .WithCustomId(op.ToString())
        //    .WithLabel("코드 실행하기")
        //    .WithStyle(ButtonStyle.Primary);
        //ComponentBuilder componentBuilder = new ComponentBuilder()
        //    .WithButton(buttonBuilder);

        var modal = new ModalBuilder()
                    .WithTitle("코드를 입력하세요")
                    .WithCustomId("Creat")
                    .AddTextInput("코드", "Creat", TextInputStyle.Paragraph, "여기에 입력");
        //await RespondAsync("코드 입력", components: componentBuilder.Build());
        await RespondWithModalAsync(modal.Build());
    }

    [SlashCommand("버그제보", "버그를 제보합니다.")]
    public async Task BugReport()
    {
 
        ModalBuilder modalBuilder = new ModalBuilder()
            .AddTextInput("어떤 상황이였나요?", "msg", TextInputStyle.Paragraph, "여기에 입력")
            .AddTextInput("버그가 난 코드 입력", "code", TextInputStyle.Paragraph, "여기에 입력")
            .WithTitle("버그 제보")
            .WithCustomId("Report");

        await RespondWithModalAsync(modalBuilder.Build());
    }
    #endregion

    #region Embed
    private readonly EmbedBuilder helpembed = new EmbedBuilder()
        .WithTitle("지원하는 명령어")
        .AddField("/도움말", "도움말 메세지를 출력합니다.")
        .AddField("/랭슝좍이란", "랭슝좍 언어에 대해서 설명합니다.")
        .AddField("/실행", "코드를 입력받아 실행합니다.")
        .AddField("/채널추가", "코드를 감지하는 채널을 추가합니다.")
        .AddField("/채널삭제", "코드를 감지하는 채널을 제거합니다.");

    private readonly EmbedBuilder infoembed = new EmbedBuilder()
        .WithTitle("문법")
        .WithDescription("랭슝좍에서 사용하는 키워드는 다음과 같습니다. 줄 단위로 명령어가 정의됩니다")
        .AddField("교주님", "랭슝좍 언어는 반드시 시작 시 '교주님'을 사용합니다.")
        .AddField("자료형", "현재는 정수형만 지원되며, '좍', '좌악', '좌아악', ...을 이용합니다.")
        .AddField("예제", "좍 : 1\n좌악 : 2\n좌아악 : 3\n좌아아악 : 4\n이하 '아'가 추가될 때마다 1씩 증가")
        .AddField("연산", "자료형-자료형, 변수-변수, 변수-자료형 사이에 사용할 수 있습니다. 덧셈 '~', 뺄셈 ';', 곱셈 ',', 나눗셈 '@'을 이용합니다. 표현의 자유도를 높이기 위해 동일한 연산자를 연속하여 사용하는 것이 가능합니다 (예제 코드 두번째 줄 참고). 연산은 우선 순위 없이 무조건 왼쪽부터 진행됩니다.")
        .AddField("예제", "좍~좍 : 1+1\n좍~~~~~~~~~~좍 : 1+1, '좍~좍'과 동일\n좌악,좌아악 : 2x3\n슝;좌아악 : 첫번째 변수에 저장된 값 - 3\n슈웅;슈우웅@좍 : (두번째 변수에 저장된 값 - 세번째 변수에 저장된 값) / 1")
        .AddField("변수 선언", "문장의 맨 앞에 '슝', '슈웅', '슈우웅', ...을 사용하여 변수를 선언합니다. 기본적으로 0의 값으로 초기화되어 있습니다. 변수 선언시에는 다른 변수를 대입하거나 정수형 자료형을 이용하여 값을 저장해야 합니다. 아무 값도 넣지 않는 경우 0을 대입합니다.")
        .AddField("예제", "슝 : 첫번째 변수에 0을 대입.\n슝좍 : 첫번째 변수에 1을 대입.\n슝...?좍!!! : 첫번째 변수에 1을 대입. (.?!를 적절히 배치)\n슈웅좌아악,좌악,좌악 : 두번째 변수에 3x2x2=12를 대입.\r\n슈우웅슈웅~슝 : 세번째 변수에 두번째 변수의 값 + 첫번째 변수의 값 = 7을 대입.")
        .AddField("출력", "출력은 ASCII와 값 출력 2가지 방법이 있습니다.")
        .AddField("ASCII", "'비비따잇'으로 문장을 시작합니다. '따잇'은 '따따잇', '따따따잇'과 같이 '잇' 앞에 붙는 '따'의 개수를 자유롭게 변경할 수 있습니다. 이후 'ㅋ'의 개수를 이용하여 변수에 저장된 값을 출력합니다. 'ㅋ'의 개수가 n인 경우 n번째 변수의 값을 출력하게 됩니다. 'ㅋ'의 위치는 자유롭게 둘 수 있습니다. 단, '비ㅋ비', '따ㅋ잇'과 같이 각 단어의 사이에 위치해서는 안됩니다.")
        .AddField("예제", "비비따잇 : SyntaxError\n비비따잇ㅋㅋ : 두 번째 변수의 값 출력\n비비ㅋ따따잇ㅋ : 두 번째 변수의 값 출력\r\n비비ㅋㅋㅋ따잇 : 세 번째 변수의 값 출력\r\n비ㅋㅋ비따잇 : SyntaxError\r\n비비따ㅋㅋㅋ잇 : SyntaxError")
        .AddField("값", "중간에 '보호막'을 추가합니다. 나머지 부분은 위와 동일합니다.")
        .AddField("예제", "비비보호막따잇 : SyntaxError\n비비보호막따잇ㅋㅋ : 두 번째 변수의 값 출력\n비비ㅋ보호막따잇ㅋ : 두 번째 변수의 값 출력\n비비보호막ㅋㅋㅋ따잇 : 세 번째 변수의 값 출력")
        .AddField("입력", "'순수따잇'으로 문장을 시작합니다. '따잇'은 '따따잇', '따따따잇'과 같이 '잇' 앞에 붙는 '따'의 개수를 자유롭게 변경할 수 있습니다. 이후 'ㅋ'의 개수를 이용하여 입력한 값을 몇번째 변수에 저장할지 지정합니다. 'ㅋ'의 개수가 n인 경우 n번째 변수에 입력값을 저장합니다. 'ㅋ'의 위치는 자유롭게 둘 수 있습니다. 단, '순ㅋ수', '따ㅋ잇'과 같이 각 단어의 사이에 위치해서는 안됩니다.")
        .AddField("If문", "{명령어} + '하는재미' + {조건}으로 구성됩니다. 만약 {조건}의 값이 0인 경우, {명령어}가 실행됩니다. {조건}이 빈 경우에도 {명령어}가 실행됩니다. {조건}의 값이 0이 아닌 경우 {명령어}는 실행되지 않습니다.")
        .AddField("예제", "비비따잇ㅋ하는재미슝 : 첫 번째 변수의 값이 0이면 첫 번째 변수의 값 출력")
        .AddField("Goto문", "'에잇'으로 문장을 시작합니다. 'ㅋ'의 개수가 이동하는 line 수를 의미합니다. '에잇'이 한번 사용된 경우 위로 이동하고, 두번 사용된 경우 아래로 이동합니다.")
        .AddField("예제", "에잇ㅋ : 1줄 위로 이동\n에잇에잇ㅋㅋㅋㅋ : 4줄 아래로 이동")
        .WithColor(Color.Green);
    #endregion
}