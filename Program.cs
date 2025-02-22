using System;
using static System.Environment;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;

class Program
{
    private DiscordSocketClient _client;
    private InteractionService _interactionService;
    private IServiceProvider _services;

    public static IUserMessage message;
    static async Task Main(string[] args) => await new Program().RunBotAsync();

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        });

        _interactionService = new InteractionService(_client);
        _client.Log += Log;
        _client.Ready += ClientReady;
        _client.MessageReceived += MesaageHandle;
        _client.InteractionCreated += HandleInteraction;

        string token = GetEnvironmentVariable("MY_BOT_TOKEN"); // 봇 토큰 입력
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task MesaageHandle(SocketMessage message)
    {
        if (message.Author.IsBot) return; 
        Console.WriteLine(message.Attachments.Count);
        if (message.Attachments.Count == 1)
        {
            var attachment = message.Attachments.First();
            string url = attachment.Url;
            string fileName = Guid.NewGuid().ToString();
            string filW = Path.GetExtension(Path.GetFileName(new Uri(url).LocalPath));
            if (filW != ".jwak")
            {
                await message.Channel.SendMessageAsync("파일의 확장자는 jwak이여야 됩니다.");
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        string content = await client.GetStringAsync(url);
                        // URL에서 파일을 비동기적으로 다운로드
                        byte[] fileBytes = await client.GetByteArrayAsync(url);

                        // 파일을 로컬에 저장
                        File.WriteAllBytes(fileName, fileBytes);

                        Console.WriteLine($"파일 '{fileName}'이 다운로드되었습니다.");
                        Console.WriteLine(content);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"파일 다운로드 중 오류가 발생했습니다: {ex.Message}");
                    }
                }
            }
    }
        else if (message.Attachments.Count == 0)
        {
            await message.Channel.SendMessageAsync("파일을 하나 보내야 됩니다.");
        }
        else
        {
            await message.Channel.SendMessageAsync("파일을 하나만 보내야 됩니다.");
        }
    }

    private async Task ClientReady()
    {
        await _interactionService.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _services);
        await _interactionService.RegisterCommandsGloballyAsync();
        Console.WriteLine("Bot is Ready!");
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        if (interaction is SocketMessageComponent component)
        {
            if (component.Data.CustomId == "answer_select")
            {
                // 사용자가 선택한 옵션 가져오기
                var answer = component.Data.Values.FirstOrDefault() ?? "응답 없음";
                await component.RespondAsync($"선택한 답변: {answer}", ephemeral: true);
            }
            else if (component.Data.CustomId == "open_modal")
            {
                // 버튼을 눌렀을 때 모달 열기
                var modal = new ModalBuilder()
                    .WithTitle("질문에 답변하기")
                    .WithCustomId("answer_modal")
                    .AddTextInput("답변을 입력하세요:", "answer_input", TextInputStyle.Paragraph, "여기에 입력");

                await component.RespondWithModalAsync(modal.Build());
            }
        }
        else if (interaction is SocketModal modal)
        {
            var answerInput = modal.Data.Components.FirstOrDefault(x => x.CustomId == "answer_input")?.Value ?? "응답 없음";
            await modal.RespondAsync($"답변: {answerInput}");
            // 메시지 수정 (기존 질문에 답변 추가)
            if (modal.Channel is IMessageChannel channel)
            {
                //var message = await channel.GetMessageAsync(originalMessageId) as IUserMessage;
                if (message != null)
                {
                    Console.WriteLine("들어왔는데");
                    await message.ModifyAsync(msg => msg.Content = $"질문: 이 질문에 대한 답변은?\n**답변: {answerInput}**");
                }
            }
        }
        else if (interaction is SocketSlashCommand command)
        {
            await _interactionService.ExecuteCommandAsync(new SocketInteractionContext(_client, interaction), _services);
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg);
        return Task.CompletedTask;
    }
}

public class QuestionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("질문", "질문을 합니다.")]
    public async Task AskQuestion()
    {
        // 선택 메뉴 생성
        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("answer_select")
            .WithPlaceholder("답변을 선택하세요")
            .AddOption("네", "yes")
            .AddOption("아니오", "no")
            .AddOption("모름", "unknown");

        // 직접 입력 버튼 생성
        var button = new ButtonBuilder()
            .WithLabel("직접 입력")
            .WithStyle(ButtonStyle.Primary)
            .WithCustomId("open_modal");

        // 컴포넌트 빌드 (선택 메뉴 + 버튼)
        var component = new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .WithButton(button);

        // 질문 메시지 보내기
        await RespondAsync("질문: 이 질문에 대한 답변은?", components: component.Build());

        var message = GetOriginalResponseAsync().Result;
        Program.message = message;
        // 메시지 ID 저장 (사용자 별로 답변을 추적하기 위해 사용)
        var userId = Context.User.Id;
        // 여기에 메시지 ID를 저장해두면 나중에 해당 메시지를 수정할 수 있습니다.
    }
}
