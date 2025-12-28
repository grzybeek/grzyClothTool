using System;
using System.IO;
using System.Xml.Linq;

namespace grzyClothTool.Helpers;

public static class ErrorLogHelper
{
    private static readonly string LogFileName = "grzyClothTool_errors.log";
    private static readonly object _lockObject = new();

    public static void LogError(string message, Exception ex = null)
    {
        try
        {
            lock (_lockObject)
            {
                var logFilePath = GetLogFilePath();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] {message}";

                if (ex != null)
                {
                    logEntry += $"\nException: {ex.GetType().Name}: {ex.Message}";
                    logEntry += $"\nStackTrace: {ex.StackTrace}";
                    
                    if (ex.InnerException != null)
                    {
                        logEntry += $"\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                        logEntry += $"\nInner StackTrace: {ex.InnerException.StackTrace}";
                    }
                }

                logEntry += "\n" + new string('-', 80) + "\n";

                File.AppendAllText(logFilePath, logEntry);
                LogHelper.Log(message, Views.LogType.Warning);
            }
        }
        catch
        {
            // Silently fail if we can't write to the log file
        }
    }

    private static string GetLogFilePath()
    {
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(exeDirectory, LogFileName);
    }

    public static string GetLogFileLocation()
    {
        return GetLogFilePath();
    }
}
