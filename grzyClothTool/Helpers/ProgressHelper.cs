using System;
using System.Diagnostics;

namespace grzyClothTool.Helpers;

public enum ProgressStatus
{
    Start,
    Stop
}

public class ProgressMessageEventArgs : EventArgs
{
    public ProgressStatus Status { get; set; }
}

public static class ProgressHelper
{
    private static Stopwatch timer;
    public static event EventHandler<ProgressMessageEventArgs> ProgressStatusChanged;

    /// <summary>
    /// Starts the progress timer and optionally logs a message.
    /// </summary>
    /// <param name="logText"></param>
    public static void Start(string logText = null)
    {
        SaveHelper.SavingPaused = true;
        StartTimer();

        if (!string.IsNullOrEmpty(logText))
        {
            LogHelper.Log(logText);
        }

        ProgressStatusChanged?.Invoke(null, new ProgressMessageEventArgs { Status = ProgressStatus.Start });
    }

    /// <summary>
    /// Stops the progress timer, optionally logs a message, and optionally formats the elapsed time to be included in the log message.
    /// </summary>
    /// <param name="logText">The log message to be displayed. If null or empty, nothing is displayed.</param>
    /// <param name="formatTime">If true, the elapsed time is formatted and included in the log message.</param>
    public static void Stop(string logText = null, bool formatTime = false)
    {
        SaveHelper.SavingPaused = false;
        ProgressStatusChanged?.Invoke(null, new ProgressMessageEventArgs { Status = ProgressStatus.Stop });

        var elapsedTime = StopTimer();
        if (!string.IsNullOrEmpty(logText))
        {
            if(formatTime)
            {
                logText = string.Format(logText, elapsedTime);
            }

            LogHelper.Log(logText);
        }
    }

    private static void StartTimer()
    {
        timer = new Stopwatch();
        timer.Start();
    }

    private static string StopTimer()
    {
        timer.Stop();
        return timer.Elapsed.ToString(@"hh\:mm\:ss\.fff");
    }
}
