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
        public const string AutoSaveFileName = "autosave.json";
        public const string AutoSaveExternalFileName = "autosave.external.json";
        public static string GetSaveFileName(bool isExternalProject)
        {
            return isExternalProject ? AutoSaveExternalFileName : AutoSaveFileName;
        }
        
        public static bool ProjectExists(string mainProjectsFolder, string projectName, out bool isExternal)
        {
            isExternal = false;
            
            if (string.IsNullOrEmpty(mainProjectsFolder) || string.IsNullOrEmpty(projectName))
                return false;
                
            var projectPath = Path.Combine(mainProjectsFolder, projectName.Trim());
            
            if (File.Exists(Path.Combine(projectPath, AutoSaveFileName)))
                return true;
                
            if (File.Exists(Path.Combine(projectPath, AutoSaveExternalFileName)))
            {
                isExternal = true;
                return true;
            }
            
            return false;
        }

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
        get 
        { 
            return new JsonSerializerOptions { WriteIndented = true };
        }
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
                var isExternalProject = MainWindow.AddonManager.IsExternalProject;

                if (!string.IsNullOrEmpty(mainProjectsFolder) && 
                    !string.IsNullOrEmpty(projectName) && 
                    Directory.Exists(mainProjectsFolder))
                {
                    var projectFolder = Path.Combine(mainProjectsFolder, projectName);
                    Directory.CreateDirectory(projectFolder);

                    var saveFileName = GetSaveFileName(isExternalProject);
                    var autoSavePath = Path.Combine(projectFolder, saveFileName);
                    await WriteAutoSaveWithBackupAsync(autoSavePath, json);

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

    private static async Task WriteAutoSaveWithBackupAsync(string autoSavePath, string json)
    {
        var directory = Path.GetDirectoryName(autoSavePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Auto-save path directory is invalid.");
        }

        var fileName = Path.GetFileName(autoSavePath);
        var tempPath = Path.Combine(directory, $"{fileName}.{Guid.NewGuid():N}.tmp");
        var backupPath = $"{autoSavePath}.bak";
        var hadOriginal = false;

        try
        {
            await File.WriteAllTextAsync(tempPath, json);

            if (File.Exists(autoSavePath))
            {
                hadOriginal = true;

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Move(autoSavePath, backupPath);
            }

            // Atomic rename in same directory.
            File.Move(tempPath, autoSavePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            // If rotation already happened and new write failed, restore the previous autosave.
            if (hadOriginal && !File.Exists(autoSavePath) && File.Exists(backupPath))
            {
                File.Move(backupPath, autoSavePath, overwrite: true);
            }

            throw;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
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
            var addonManager = JsonSerializer.Deserialize<AddonManager>(json, SerializerOptions) ?? throw new InvalidOperationException("Failed to deserialize save file.");

            var fileName = Path.GetFileName(filePath);
            var isExternalFromFileName = fileName.Equals(AutoSaveExternalFileName, StringComparison.OrdinalIgnoreCase);
            
            var isExternalProject = addonManager.IsExternalProject || isExternalFromFileName;

            foreach (var addon in addonManager.Addons)
            {
                foreach (var drawable in addon.Drawables)
                {
                    if (!string.IsNullOrEmpty(drawable.FilePath) && drawable.FilePath.Contains("reservedDrawable.ydd"))
                    {
                        drawable.IsReserved = true;
                    }
                }
            }

            MainWindow.AddonManager.Addons.Clear();
            foreach (var addon in addonManager.Addons)
            {
                MainWindow.AddonManager.Addons.Add(addon);
            }

            MainWindow.AddonManager.ProjectName = addonManager.ProjectName;
            MainWindow.AddonManager.IsExternalProject = isExternalProject;

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
                addonCount,
                isExternal: isExternalProject
            );

            LogHelper.Log("Scanning project for duplicate drawables...");
            DuplicateDetector.Clear();
            
            foreach (var addon in MainWindow.AddonManager.Addons)
            {
                foreach (var drawable in addon.Drawables)
                {
                    DuplicateDetector.RegisterDrawable(drawable);
                }
            }
            
            LogHelper.Log($"Duplicate scan complete. Found {DuplicateDetector.GetDuplicateGroupCount()} duplicate drawable groups.");
            LogHelper.Log($"Loaded save from: {filePath}");
        }
        finally
        {
            FileHelper.ClearLoadContext();
        }
    }
}
