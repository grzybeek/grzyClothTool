using grzyClothTool.Extensions;
using grzyClothTool.Models;
using System;
using System.Collections.ObjectModel;
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
    private static int _saveCounter = 0;
    public static event Action SaveCreated;
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
        _timer = new Timer(60000);
        _timer.Elapsed += async (sender, e) => await SaveAsync();
        _timer.Start();

        var latestSaveFile = Directory.EnumerateFiles(SavesPath, "save_*.json")
            .Select(file => new { File = file, WriteTime = File.GetLastWriteTime(file) })
            .OrderBy(fileInfo => fileInfo.WriteTime)
            .FirstOrDefault();

        if (latestSaveFile != null)
        {
            var fileName = Path.GetFileNameWithoutExtension(latestSaveFile.File);
            var numberPart = fileName.Split('_').Last();
            if (int.TryParse(numberPart, out var number))
            {
                _saveCounter = number;
            }
        }
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

            var filename = $"save_{_saveCounter}.json";
            var path = Path.Combine(SavesPath, filename);

            await File.WriteAllTextAsync(path, json);

            LogHelper.Log($"Saved in {timer.ElapsedMilliseconds}ms");
            _saveCounter = (_saveCounter + 1) % 10;

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

    public static async Task LoadAsync(SaveFile save)
    {
        var path = Path.Combine(SavesPath, $"{save.FileName}.json");

        var json = await File.ReadAllTextAsync(path);
        var addonManager = JsonSerializer.Deserialize<AddonManager>(json);

        MainWindow.AddonManager.Addons.Clear();
        foreach (var addon in addonManager.Addons)
        {
            MainWindow.AddonManager.Addons.Add(addon);
        }

        GroupManager.Instance.Groups.Clear();
        foreach (var group in addonManager.Groups)
        {
            GroupManager.Instance.Groups.Add(group);
        }

        MainWindow.AddonManager.ProjectName = addonManager.ProjectName;

        LogHelper.Log($"Loaded save: {save.SaveDate}");
    }

    public static ObservableCollection<SaveFile> GetSaveFiles()
    {
        var files = Directory.EnumerateFiles(SavesPath, "*.json")
            .Select(file => new SaveFile
            {
                FileName = Path.GetFileNameWithoutExtension(file),
                SaveDate = File.GetLastWriteTime(file)
            })
            .ToObservableCollection();

        return files;
    }

}
