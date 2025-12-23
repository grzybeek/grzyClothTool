using grzyClothTool.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static grzyClothTool.Controls.CustomMessageBox;
using Timer = System.Timers.Timer;

namespace grzyClothTool.Helpers;

public class SaveFile
{
    public string FileName { get; set; }
    public DateTime SaveDate { get; set; }
}

public static class SaveHelper
{

    public static string SavesPath { get; private set; }
    private static Timer _timer;
    public static event Action SaveCreated;

    public static event Action<double> AutoSaveProgress;
    public static event Action<int> RemainingSecondsChanged;
    private static int _autoSaveInterval = 60000; // 60 seconds
    private static int _elapsedTime = 0;

    private static SemaphoreSlim _semaphore = new(1);

    public static bool HasUnsavedChanges { get; set; }
    public static bool SavingPaused { get; set; }

    public static JsonSerializerOptions SerializerOptions
    {
        get { return new JsonSerializerOptions { WriteIndented = true }; }
    }

    static SaveHelper()
    {
        var appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var exeName = Assembly.GetExecutingAssembly().GetName().Name;

        SavesPath = Path.Combine(appdataPath, exeName, "saves");
        Directory.CreateDirectory(SavesPath);
    }

    public static void Init()
    {
        _timer = new Timer(100);
        _timer.Elapsed += OnAutoSaveTick;
        _timer.Start();
    }

    private static async void OnAutoSaveTick(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (SavingPaused || !HasUnsavedChanges)
        {
            _elapsedTime = 0;
            AutoSaveProgress?.Invoke(0);
            RemainingSecondsChanged?.Invoke(0);
            return;
        }

        _elapsedTime += (int)_timer.Interval;
        double percentage = ((double)_elapsedTime / _autoSaveInterval) * 75.0;
        int remainingSeconds = Math.Max(0, (_autoSaveInterval - _elapsedTime) / 1000);
        
        if (_elapsedTime >= _autoSaveInterval)
        {
            await SaveAsync();
            _elapsedTime = 0;
            RemainingSecondsChanged?.Invoke(0);
            return;
        }
        AutoSaveProgress?.Invoke(percentage);
        RemainingSecondsChanged?.Invoke(remainingSeconds);
    }

    public static async Task SaveAsync()
    {
        if (!HasUnsavedChanges || SavingPaused) return;

        await _semaphore.WaitAsync();

        try
        {
            var timer = new Stopwatch();
            timer.Start();
            LogHelper.Log("Started saving...");

            string json;
            lock (AddonManager.AddonsLock)
            {
                MainWindow.AddonManager.Groups.Clear();
                foreach (var group in GroupManager.Instance.Groups)
                {
                    MainWindow.AddonManager.Groups.Add(group);
                }

                json = JsonSerializer.Serialize(MainWindow.AddonManager, SerializerOptions);
            }

            try
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                var projectName = MainWindow.AddonManager.ProjectName;

                if (!string.IsNullOrEmpty(mainProjectsFolder) && 
                    !string.IsNullOrEmpty(projectName) && 
                    Directory.Exists(mainProjectsFolder))
                {
                    var projectFolder = Path.Combine(mainProjectsFolder, projectName);
                    Directory.CreateDirectory(projectFolder);

                    var autoSavePath = Path.Combine(projectFolder, "autosave.json");
                    await File.WriteAllTextAsync(autoSavePath, json);

                    LogHelper.Log($"Auto-saved to {autoSavePath} in {timer.ElapsedMilliseconds}ms");
                }
                else
                {
                    LogHelper.Log("Could not auto-save: Project folder not configured or project name not set", Views.LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Auto-save failed: {ex.Message}", Views.LogType.Error);
            }

            SaveCreated?.Invoke();
            SetUnsavedChanges(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static void SetUnsavedChanges(bool status)
    {
        HasUnsavedChanges = status;

        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            string unsavedText = " (Unsaved changes)";
            bool titleContainsUnsaved = MainWindow.Instance.Title.Contains(unsavedText);

            if (status && !titleContainsUnsaved)
            {
                MainWindow.Instance.Title += unsavedText;
            }
            else if (!status && titleContainsUnsaved)
            {
                MainWindow.Instance.Title = MainWindow.Instance.Title.Replace(unsavedText, "");
            }
        });
    }

    public static bool CheckUnsavedChangesMessage()
    {
        if (!HasUnsavedChanges) return true;

        bool result = false;

        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            var clickResult = Show("You have unsaved changes. Do you want to continue with this action?", "Unsaved changes", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.Warning);

            result = clickResult == CustomMessageBoxResult.OK;
        });

        return result;
    }


    public static async Task LoadSaveFileAsync(string filePath)
    {
        try
        {
            FileHelper.SetLoadContext(filePath);

            var json = await File.ReadAllTextAsync(filePath);
            var addonManager = JsonSerializer.Deserialize<AddonManager>(json) ?? throw new InvalidOperationException("Failed to deserialize save file.");

            MainWindow.AddonManager.Addons.Clear();
            foreach (var addon in addonManager.Addons)
            {
                MainWindow.AddonManager.Addons.Add(addon);
            }

            MainWindow.AddonManager.ProjectName = addonManager.ProjectName;

            MainWindow.AddonManager.Groups.Clear();
            if (addonManager.Groups != null)
            {
                foreach (var group in addonManager.Groups)
                {
                    MainWindow.AddonManager.Groups.Add(group);
                }
            }

            MainWindow.AddonManager.Tags.Clear();
            if (addonManager.Tags != null)
            {
                foreach (var tag in addonManager.Tags)
                {
                    MainWindow.AddonManager.Tags.Add(tag);
                }
            }

            int drawableCount = addonManager.Addons.Sum(a => a.Drawables.Count);
            int addonCount = addonManager.Addons.Count;

            PersistentSettingsHelper.Instance.AddRecentProject(
                filePath,
                addonManager.ProjectName ?? Path.GetFileNameWithoutExtension(filePath),
                drawableCount,
                addonCount
            );

            LogHelper.Log($"Loaded save from: {filePath}");
        }
        finally
        {
            FileHelper.ClearLoadContext();
        }
    }
}
