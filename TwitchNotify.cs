using Discord;
using Discord.WebSocket;
using System.Text.Json;

class Program
{
    private readonly DiscordSocketClient _client;
    private static readonly string DiscordBotToken = "123456";      //  Discord機器人憑證
    private static readonly string TwitchChannelName = "123456";    //  要追蹤的實況頻道ID
    private static readonly string TwitchClientId = "123456";       //  在https://dev.twitch.tv 申請的應用程式ClientId
    private static readonly string TwitchAccessToken = "123456";    //  Twitch存取憑證(可透過執行Get_Twitch_Token.cs取得)
    private static readonly string Notify_Role_ID = "123456";       //  標記直播通知的身分組ID
    private readonly ulong NOTIFICATION_CHANNEL_ID = 123456;        //  發送直播通知的Discord頻道ID

    private bool _isChecking = false;
    private DateTime _lastNotificationTime = DateTime.MinValue;

    //  程式進入點
    static async Task Main(string[] args)
    {
        var program = new Program();
        await program.RunAsync();
    }
    //  設定機器人基本資訊
    public Program()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            GatewayIntents = GatewayIntents.All
        });

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
    }
    //  機器人準備就緒時
    private async Task ReadyAsync()
    {
        Console.WriteLine($"Bot 已就緒! 名稱: {_client.CurrentUser.Username}");
        if (!_isChecking)
        {
            _isChecking = true;
            await StartTwitchCheck();
        }
    }
    //  啟動機器人/登入/監控事件
    public async Task RunAsync()
    {
        string token = DiscordBotToken; //  DiscordBot憑證
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(-1);
    }
    //  log紀錄
    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
    //  定期檢查檢查開台狀態
    private async Task StartTwitchCheck()
    {
        while (true)
        {
            try
            {
                var streamInfo = await GetTwitchStreamInfo(TwitchChannelName);
                if (streamInfo != null)
                {
                    // 確保至少間隔一段時間才發送新通知
                    if (DateTime.Now - _lastNotificationTime > TimeSpan.FromHours(12))
                    {
                        // 取得指定的通知頻道
                        var channel = _client.GetChannel(NOTIFICATION_CHANNEL_ID) as IMessageChannel;
                        if (channel != null)
                        {
                            var embed = new EmbedBuilder()
                                .WithTitle("📢 XXX開台拉！")
                                .WithDescription(streamInfo.Value.Title)
                                .WithColor(new Color(6570404))  //  紫色
                                .WithImageUrl($"https://static-cdn.jtvnw.net/previews-ttv/live_user_{TwitchChannelName}-1280x720.jpg")  //  縮圖
                                .WithTimestamp(DateTimeOffset.UtcNow)
                                .Build();

                            await channel.SendMessageAsync($"<@&{Notify_Role_ID}>", embed: embed);    //  標記身分組
                            _lastNotificationTime = DateTime.Now;
                            Console.WriteLine("已發送開台通知");
                        }
                        else
                        {
                            Console.WriteLine("無法找到指定的通知頻道");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"檢查Twitch狀態發生錯誤: {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromMinutes(15)); // 每15分鐘檢查一次
        }
    }
    //  取得實況資訊
    private static async Task<(string Title, string GameName)?> GetTwitchStreamInfo(string channelName)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-Id", TwitchClientId);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchAccessToken}");
        var response = await client.GetAsync($"https://api.twitch.tv/helix/streams?user_login={channelName}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var streamData = json.RootElement.GetProperty("data");

        if (streamData.GetArrayLength() > 0)
        {
            var stream = streamData[0];
            var title = stream.GetProperty("title").GetString();
            var gameName = stream.GetProperty("game_name").GetString();
            return (title, gameName);
        }
        return null;
    }
}
