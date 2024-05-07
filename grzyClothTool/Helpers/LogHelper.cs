using grzyClothTool.Views;
using System;

namespace grzyClothTool.Helpers;

public class LogMessageEventArgs : EventArgs
{
    public string TypeIcon { get; set; }
    public string Message { get; set; }
}

public static class LogHelper
{
    private static LogWindow _logWindow;
    public static event EventHandler<LogMessageEventArgs> LogMessageCreated;

    public static void Init()
    {
        _logWindow = new LogWindow();
    }

    public static void Log(string message, LogType logtype = LogType.Info)
    {
        if (_logWindow == null)
            return;

        _logWindow.Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var type = GetLogTypeIcon(logtype);

            _logWindow.LogMessages.Add(new LogMessage { TypeIcon = type, Message = message, Timestamp = timestamp });
            LogMessageCreated?.Invoke(_logWindow, new LogMessageEventArgs { TypeIcon = type, Message = message });
        });
    }

    public static string GetLogTypeIcon(LogType type)
    {
        return type switch
        {
            LogType.Info => "Check",
            LogType.Warning => "WarningOutline",
            LogType.Error => "Close",
            _ => "Info"
        };
    }

    public static void OpenLogWindow()
    {
        _logWindow.Show();
    }

    public static void Close()
    {
        _logWindow.Closing -= _logWindow.LogWindow_Closing;
        _logWindow.Close();
        _logWindow = null;
    }
}
