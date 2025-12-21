using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace grzyClothTool.Helpers;

/// <summary>
/// Manages application settings that persist across updates.
/// Settings are stored in %LocalAppData%\grzyClothTool\settings.json
/// </summary>
public class PersistentSettingsHelper
{
    private static readonly Lazy<PersistentSettingsHelper> _instance = new(() => new PersistentSettingsHelper());
    public static PersistentSettingsHelper Instance => _instance.Value;

    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private PersistentSettings _settings;

    private PersistentSettingsHelper()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "grzyClothTool"
        );
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<PersistentSettings>(json) ?? new PersistentSettings();
            }
            else
            {
                _settings = new PersistentSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading persistent settings: {ex.Message}");
            _settings = new PersistentSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving persistent settings: {ex.Message}");
        }
    }

    public bool IsFirstRun
    {
        get => _settings.IsFirstRun;
        set
        {
            if (_settings.IsFirstRun != value)
            {
                _settings.IsFirstRun = value;
                SaveSettings();
            }
        }
    }

    public string MainProjectsFolder
    {
        get => _settings.MainProjectsFolder ?? string.Empty;
        set
        {
            if (_settings.MainProjectsFolder != value)
            {
                _settings.MainProjectsFolder = value;
                SaveSettings();
            }
        }
    }

    public string SettingsFilePath => _settingsFilePath;

    public List<RecentProject> RecentlyOpenedProjects
    {
        get => _settings.RecentlyOpenedProjects ?? new List<RecentProject>();
        set
        {
            _settings.RecentlyOpenedProjects = value;
            SaveSettings();
        }
    }


    public void AddRecentProject(string filePath, string projectName, int drawableCount, int addonCount)
    {
        var recentProjects = RecentlyOpenedProjects;
        
        recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        
        recentProjects.Insert(0, new RecentProject
        {
            FilePath = filePath,
            ProjectName = projectName,
            LastModified = DateTime.Now,
            DrawableCount = drawableCount,
            AddonCount = addonCount
        });
        
        if (recentProjects.Count > 3)
        {
            recentProjects = [.. recentProjects.Take(3)];
        }
        
        RecentlyOpenedProjects = recentProjects;
    }

    public static bool IsRootDrive(string path)
    {
        try
        {
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string root = Path.GetPathRoot(normalizedPath);

            return string.Equals(normalizedPath, root?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}


public class PersistentSettings
{
    public bool IsFirstRun { get; set; } = true;
    public string MainProjectsFolder { get; set; } = string.Empty;
    public List<RecentProject> RecentlyOpenedProjects { get; set; } = [];
}

public class RecentProject
{
    public string FilePath { get; set; }
    public string ProjectName { get; set; }
    public DateTime LastModified { get; set; }
    public int DrawableCount { get; set; }
    public int AddonCount { get; set; }
}
