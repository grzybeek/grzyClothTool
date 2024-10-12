using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

public class SessionEvent
{
    public string UserHash { get; set; }
    public string SessionType { get; set; }
    public DateTime SessionTime { get; set; }
    public string UniqueSessionId { get; set; }
}

public class TelemetryHelper
{
    private readonly static Guid _uniqueSessionId = Guid.NewGuid();

    public static async Task LogSession(bool isSessionStart)
    {
        var se = new SessionEvent
        {
            UserHash = ObfuscationHelper.HashString(Environment.MachineName),
            SessionType = isSessionStart ? "start" : "end",
            SessionTime = DateTime.UtcNow,
            UniqueSessionId = _uniqueSessionId.ToString()
        };

        await SendLogEventAsync(se);
    }


    private static async Task SendLogEventAsync(SessionEvent logEvent)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(logEvent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await App.httpClient.PostAsync("https://www.grzybeek.pl/grzyClothTool/log", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            // ignore?
        }
    }
}
