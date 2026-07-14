using grzyClothTool.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
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

public class SaveBackupFile
{
    public string FilePath { get; set; }
    public DateTime CreatedAt { get; set; }
}

    public static class SaveHelper
    {
        public const string AutoSaveFileName = "autosave.json";
        public const string AutoSaveExternalFileName = "autosave.external.json";
        private const string BackupFolderName = "save-backups";
        private const int MaxBackupVersions = 5;
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

   
    private static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

    public static JsonSerializerOptions SerializerOptions => _serializerOptions;

    static SaveHelper()
    {
        var appdataPath = AppDataHelper.GetLocalAppDataPath();
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

    private static int _pausedWithChangesTime = 0;
    private static bool _pausedWarningLogged = false;

    private static async void OnAutoSaveTick(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (SavingPaused || !HasUnsavedChanges)
        {
            if (SavingPaused && HasUnsavedChanges)
            {
                _pausedWithChangesTime += (int)_timer.Interval;
                if (!_pausedWarningLogged && _pausedWithChangesTime >= _autoSaveInterval)
                {
                    _pausedWarningLogged = true;
                    LogHelper.Log("Auto-save has been paused for over a minute while changes are pending. It will resume when the current operation finishes.", Views.LogType.Warning);
                }
            }
            else
            {
                _pausedWithChangesTime = 0;
                _pausedWarningLogged = false;
            }

            _elapsedTime = 0;
            AutoSaveProgress?.Invoke(0);
            RemainingSecondsChanged?.Invoke(0);
            return;
        }

        _pausedWithChangesTime = 0;
        _pausedWarningLogged = false;

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

            try
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                var projectName = MainWindow.AddonManager.ProjectName;
                var isExternalProject = MainWindow.AddonManager.IsExternalProject;

                if (string.IsNullOrEmpty(mainProjectsFolder) ||
                    string.IsNullOrEmpty(projectName) ||
                    !Directory.Exists(mainProjectsFolder))
                {
                    LogHelper.Log("Could not auto-save: Project folder not configured or project name not set", Views.LogType.Warning);
                    return;
                }

                var projectFolder = Path.Combine(mainProjectsFolder, projectName);
                Directory.CreateDirectory(projectFolder);

                var saveFileName = GetSaveFileName(isExternalProject);
                var autoSavePath = Path.Combine(projectFolder, saveFileName);
                await RotateSaveBackupAsync(autoSavePath);
                await WriteSaveFileAtomicallyAsync(autoSavePath);

                LogHelper.Log($"Auto-saved to {autoSavePath} in {timer.ElapsedMilliseconds}ms");
                SaveCreated?.Invoke();
                SetUnsavedChanges(false);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Auto-save failed: {ex.Message}", Views.LogType.Error);
            }
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
            await LoadSaveFileCoreAsync(filePath);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Failed to load save from {filePath}: {ex.Message}", Views.LogType.Error);

            var backupFiles = GetBackupSaveFiles(filePath);
            if (backupFiles.Count == 0)
            {
                throw;
            }

            var useBackup = false;
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                var newestBackup = backupFiles[0];
                var message =
                    $"This save file could not be loaded and is probably broken.\n\n" +
                    $"Save file:\n{filePath}\n\n" +
                    $"Error:\n{ex.Message}\n\n" +
                    $"Found {backupFiles.Count} backup save(s). The newest backup was created on {newestBackup.CreatedAt:g}.\n\n" +
                    "Do you want to load the newest working backup instead?";

                var result = Show(message, "Broken Save File", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Warning);
                useBackup = result == CustomMessageBoxResult.Yes;
            });

            if (!useBackup)
            {
                throw;
            }

            Exception lastBackupException = null;
            foreach (var backupFile in backupFiles)
            {
                try
                {
                    await LoadSaveFileCoreAsync(backupFile.FilePath, filePath);
                    LogHelper.Log($"Recovered project from backup save: {backupFile.FilePath}", Views.LogType.Warning);

                    MainWindow.Instance.Dispatcher.Invoke(() =>
                    {
                        Show($"Loaded backup save:\n{backupFile.FilePath}", "Backup Loaded", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Information);
                    });

                    return;
                }
                catch (Exception backupException)
                {
                    lastBackupException = backupException;
                    LogHelper.Log($"Backup save failed to load ({backupFile.FilePath}): {backupException.Message}", Views.LogType.Error);
                }
            }

            throw new InvalidOperationException("The original save file and all available backups failed to load.", lastBackupException ?? ex);
        }
    }

    private static async Task LoadSaveFileCoreAsync(string filePath, string recentProjectFilePath = null)
    {
        try
        {
            FileHelper.SetLoadContext(filePath);

            AddonManager addonManager;
            await using (var stream = File.OpenRead(filePath))
            {
                addonManager = await Task.Run(async () =>
                    await JsonSerializer.DeserializeAsync<AddonManager>(stream, SerializerOptions))
                    ?? throw new InvalidOperationException("Failed to deserialize save file.");
            }

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
                recentProjectFilePath ?? filePath,
                addonManager.ProjectName ?? Path.GetFileNameWithoutExtension(filePath),
                drawableCount,
                addonCount,
                isExternal: isExternalProject
            );

            LogHelper.Log("Scanning project for duplicate drawables in the background...");
            DuplicateDetector.Clear();
            var scanVersion = DuplicateDetector.Version;
            var drawablesToScan = MainWindow.AddonManager.Addons.SelectMany(a => a.Drawables).ToList();

            
            _ = Task.Run(async () =>
            {
                try
                {
                    var loadTasks = drawablesToScan.Where(d => !d.IsReserved).Select(d => d.DetailsLoaded).ToArray();
                    var allLoaded = Task.WhenAll(loadTasks);
                    await Task.WhenAny(allLoaded, Task.Delay(TimeSpan.FromMinutes(5)));

                    await MainWindow.Instance.Dispatcher.InvokeAsync(() =>
                    {
                        if (DuplicateDetector.Version != scanVersion)
                        {
                            return;
                        }

                        foreach (var drawable in drawablesToScan)
                        {
                            DuplicateDetector.RegisterDrawable(drawable);
                        }

                        LogHelper.Log($"Duplicate scan complete. Found {DuplicateDetector.GetDuplicateGroupCount()} duplicate drawable groups.");
                    }, System.Windows.Threading.DispatcherPriority.Background);

                    // Loading parses many YDD files whose buffers land on the large object heap;
                    // compact it once so the freed memory is actually returned to the OS.
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Background duplicate scan failed: {ex.Message}", Views.LogType.Warning);
                }
            });

            LogHelper.Log($"Loaded save from: {filePath}");
        }
        finally
        {
            FileHelper.ClearLoadContext();
        }
    }

    private static async Task RotateSaveBackupAsync(string saveFilePath)
    {
        if (!File.Exists(saveFilePath))
        {
            return;
        }

        try
        {
            var backupFolder = GetBackupFolder(saveFilePath);
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            var backupFileName = $"{Path.GetFileNameWithoutExtension(saveFilePath)}.{timestamp}.json";
            var backupPath = Path.Combine(backupFolder, backupFileName);

            File.Copy(saveFilePath, backupPath, overwrite: false);
            await Task.Run(() => PruneOldBackups(saveFilePath));
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Could not create save backup: {ex.Message}", Views.LogType.Warning);
        }
    }

    private static async Task WriteSaveFileAtomicallyAsync(string saveFilePath)
    {
        var projectFolder = Path.GetDirectoryName(saveFilePath) ?? throw new InvalidOperationException("Save file has no parent folder.");
        var tempPath = Path.Combine(projectFolder, $"{Path.GetFileName(saveFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            // Serialize straight to the temp file instead of building a multi-MB string first.
            using (var stream = File.Create(tempPath))
            {
                lock (AddonManager.AddonsLock)
                {
                    MainWindow.AddonManager.Groups.Clear();
                    foreach (var group in GroupManager.Instance.Groups)
                    {
                        MainWindow.AddonManager.Groups.Add(group);
                    }

                    JsonSerializer.Serialize(stream, MainWindow.AddonManager, SerializerOptions);
                }
            }

            
            await using (var validationStream = File.OpenRead(tempPath))
            {
                using var _ = await JsonDocument.ParseAsync(validationStream);
            }

            if (File.Exists(saveFilePath))
            {
                File.Replace(tempPath, saveFilePath, null);
            }
            else
            {
                File.Move(tempPath, saveFilePath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public static List<SaveBackupFile> GetBackupSaveFiles(string saveFilePath)
    {
        var backupFolder = GetBackupFolder(saveFilePath);
        if (!Directory.Exists(backupFolder))
        {
            return [];
        }

        var saveName = Path.GetFileNameWithoutExtension(saveFilePath);
        return Directory
            .GetFiles(backupFolder, $"{saveName}.*.json")
            .Select(path => new SaveBackupFile
            {
                FilePath = path,
                CreatedAt = File.GetCreationTime(path)
            })
            .OrderByDescending(file => file.CreatedAt)
            .ToList();
    }

    private static void PruneOldBackups(string saveFilePath)
    {
        var backupsToDelete = GetBackupSaveFiles(saveFilePath)
            .Skip(MaxBackupVersions)
            .ToList();

        foreach (var backupFile in backupsToDelete)
        {
            try
            {
                File.Delete(backupFile.FilePath);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Could not delete old save backup {backupFile.FilePath}: {ex.Message}", Views.LogType.Warning);
            }
        }
    }

    private static string GetBackupFolder(string saveFilePath)
    {
        var projectFolder = Path.GetDirectoryName(saveFilePath) ?? SavesPath;
        return Path.Combine(projectFolder, BackupFolderName);
    }
}
