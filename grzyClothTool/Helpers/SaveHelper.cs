using grzyClothTool.Extensions;
using grzyClothTool.Models;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static grzyClothTool.Controls.CustomMessageBox;
using Timer = System.Timers.Timer;

namespace grzyClothTool.Helpers;

public class SaveFile
{
    public string FullPath { get; set; }
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

        await SaveAsync(null);
    }

    public static async Task SaveAsync(string path)
    {
        await _semaphore.WaitAsync();

        try
        {
            var timer = new Stopwatch();
            timer.Start();
            LogHelper.Log("Started saving...");

            var json = JsonConvert.SerializeObject(MainWindow.AddonManager, Formatting.Indented);
            if (path == null)
            {
                var filename = $"save_{_saveCounter}.json";
                _saveCounter = (_saveCounter + 1) % 10;
                path = Path.Combine(SavesPath, filename);
            }

            await File.WriteAllTextAsync(path, json);

            LogHelper.Log($"Saved in {timer.ElapsedMilliseconds}ms");

            SaveCreated?.Invoke();
            SetUnsavedChanges(false);
        }
        catch (Exception e)
        {
            LogHelper.Log("ERROR: Failed to create save file... " + e.Message);
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
        var json = await File.ReadAllTextAsync(save.FullPath);
        var addonManager = JsonConvert.DeserializeObject<AddonManager>(json);

        MainWindow.AddonManager.Addons.Clear();
        foreach (var addon in addonManager.Addons)
        {
            MainWindow.AddonManager.Addons.Add(addon);
        }

        LogHelper.Log($"Loaded save: {save.SaveDate}");
        SetUnsavedChanges(false);
    }

    public static ObservableCollection<SaveFile> GetSaveFiles()
    {
        var files = Directory.EnumerateFiles(SavesPath, "*.json")
            .Select(GetFileAsSaveFile)
            .ToObservableCollection();

        return files;
    }

    public static SaveFile GetFileAsSaveFile(string file)
    {
        return new SaveFile
        {
            FullPath = file,
            FileName = Path.GetFileNameWithoutExtension(file),
            SaveDate = File.GetLastWriteTime(file)
        };
    }

}
