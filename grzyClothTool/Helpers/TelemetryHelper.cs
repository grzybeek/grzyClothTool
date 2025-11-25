using grzyClothTool.Constants;
using Sentry;
using System;
using System.IO;
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
            var response = await App.httpClient.PostAsync($"{GlobalConstants.GRZY_TOOLS_URL}/grzyClothTool/log", content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException)
        {
            // ignore?
        }
    }

    public static void CaptureExceptionWithAttachment(Exception ex, string path)
    {
        if (File.Exists(path))
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra("AttachedFileName", Path.GetFileName(path));
                scope.AddAttachment(stream, Path.GetFileName(path));
            });
        }
        else
        {
            SentrySdk.CaptureException(ex);
        }
    }
}
