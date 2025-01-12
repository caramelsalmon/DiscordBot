using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly string TwitchClientId = "123456";       //  在https://dev.twitch.tv 申請的應用程式ClientId
    private static readonly string TwitchClientSecret = "456789";   //  在https://dev.twitch.tv 申請的應用程式密碼

    static async Task Main(string[] args)
    {
        var accessToken = await GetAccessToken();
        Console.WriteLine($"Access Token: {accessToken}");
    }

    private static async Task<string> GetAccessToken()
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", TwitchClientId),
                new KeyValuePair<string, string>("client_secret", TwitchClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            })
        };

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        return json.RootElement.GetProperty("access_token").GetString();
    }
}
