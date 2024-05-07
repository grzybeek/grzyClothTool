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

        _ = SaveAsync();
    }



    public static async Task SaveAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            var timer = new Stopwatch();
            timer.Start();
            LogHelper.Log("Started saving...");

            var json = JsonConvert.SerializeObject(MainWindow.AddonManager, Formatting.Indented);
            var filename = $"save_{_saveCounter}.json";
            var path = Path.Combine(SavesPath, filename);

            await File.WriteAllTextAsync(path, json);

            LogHelper.Log($"Saved in {timer.ElapsedMilliseconds}ms");
            _saveCounter = (_saveCounter + 1) % 10;

            SaveCreated?.Invoke();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task LoadAsync()
    {

        foreach (var file in Directory.EnumerateFiles(SavesPath, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file);
            var addon = JsonConvert.DeserializeObject<Addon>(json);

            MainWindow.AddonManager.Addons.Clear();
            MainWindow.AddonManager.Addons.Add(addon);
        }
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
