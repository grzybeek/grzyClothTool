using grzyClothTool.Constants;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private List<string> _patreonList;
        public List<string> PatreonList
        {
            get => _patreonList;
            set
            {
                _patreonList = value;
                OnPropertyChanged(nameof(PatreonList));
            }
        }

        private string _latestVersion;
        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged(nameof(LatestVersion));
            }
        }

        private List<string> _changelogHighlights;
        public List<string> ChangelogHighlights
        {
            get => _changelogHighlights;
            set
            {
                _changelogHighlights = value;
                OnPropertyChanged(nameof(ChangelogHighlights));
            }
        }

        private List<ToolInfo> _otherTools;
        public List<ToolInfo> OtherTools
        {
            get => _otherTools;
            set
            {
                _otherTools = value;
                OnPropertyChanged(nameof(OtherTools));
            }
        }

        private ObservableCollection<RecentProject> _recentlyOpened;
        public ObservableCollection<RecentProject> RecentlyOpened
        {
            get => _recentlyOpened;
            set
            {
                _recentlyOpened = value;
                OnPropertyChanged(nameof(RecentlyOpened));
                OnPropertyChanged(nameof(ShowNoRecentProjects));
            }
        }

        public bool ShowNoRecentProjects => RecentlyOpened == null || RecentlyOpened.Count == 0;

        private readonly List<string> didYouKnowStrings = [
            "You can open any existing addon and it will load all properties such as heels or hats.",
            "You can export an existing project when you are not finished and later import it to continue working on it.",
            "There is switch to enable dark theme in the settings.",
            "There is 'live texture' feature in 3d preview? It allows you to see how your texture looks on the model in real time, even after changes.",
            "You can click SHIFT + DEL to instantly delete a selected drawable, without popup.",
            "You can click CTRL + DEL to instantly replace a selected drawable with reserved drawable.",
            "You can reserve your drawables and later change it to real model.",
            "Supporting me with monthly patreon will speed up the development of the tool!",
            "You can hover over warning icon to see what is wrong with your drawable or texture.",
        ];

        public string RandomDidYouKnow => didYouKnowStrings[new Random().Next(0, didYouKnowStrings.Count)];

        public Home()
        {
            InitializeComponent();
            DataContext = this;

            OtherTools = [
                new ToolInfo
                {
                    Name = "grzyOptimizer",
                    Description = "Optimize YDD models, reduce polygon and vertex count while maintaining visual quality.",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                },
                new ToolInfo
                {
                    Name = "grzyTattooTool",
                    Description = "Create and edit tattoos with preview and quick addon resource generation for FiveM.",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                }
            ];

            LoadRecentProjects();

            Loaded += Home_Loaded;
        }

        private void LoadRecentProjects()
        {
            var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
            var validProjects = recentProjects.Where(p => File.Exists(p.FilePath)).ToList();
            
            if (validProjects.Count != recentProjects.Count)
            {
                PersistentSettingsHelper.Instance.RecentlyOpenedProjects = validProjects;
            }
            
            RecentlyOpened = new ObservableCollection<RecentProject>(validProjects);
        }

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await FetchPatreons();
            } 
            catch
            {
                PatreonList = ["Failed to fetch patreons"];
            }

            try
            {
                await FetchLatestRelease();
            }
            catch
            {
                LatestVersion = "Unable to fetch version";
                ChangelogHighlights = ["Failed to load changelog highlights"];
            }
        }

        private async Task FetchPatreons()
        {
            var url = $"{GlobalConstants.GRZY_TOOLS_URL}/grzyClothTool/patreons";

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                PatreonList = JsonSerializer.Deserialize<List<string>>(content);
            }
        }

        private async Task FetchLatestRelease()
        {
            var url = "https://api.github.com/repos/grzybeek/grzyClothTool/releases/latest";

            App.httpClient.DefaultRequestHeaders.UserAgent.Clear();
            App.httpClient.DefaultRequestHeaders.Add("User-Agent", "grzyClothTool");

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var release = JsonSerializer.Deserialize<JsonElement>(content);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    if (release.TryGetProperty("tag_name", out var tagName))
                    {
                        LatestVersion = tagName.GetString();
                    }

                    if (release.TryGetProperty("body", out var body))
                    {
                        ChangelogHighlights = ParseChangelogHighlights(body.GetString());
                    }
                });
            }
        }

        private static List<string> ParseChangelogHighlights(string changelogBody)
        {
            if (string.IsNullOrWhiteSpace(changelogBody))
                return ["No changelog available"];

            var highlights = new List<string>();
            var lines = changelogBody.Split(['\r', '\n'], StringSplitOptions.None);
            var inChangelogSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Changelog", StringComparison.OrdinalIgnoreCase))
                {
                    inChangelogSection = true;
                    continue;
                }

                if (inChangelogSection && string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (inChangelogSection && trimmedLine.StartsWith("##"))
                {
                    break;
                }

                if (inChangelogSection &&
                    (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*") || trimmedLine.StartsWith("•")))
                {
                    var cleanLine = trimmedLine.TrimStart('-', '*', '•', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        highlights.Add(cleanLine);

                        if (highlights.Count >= 10)
                            break;
                    }
                }
            }

            return highlights.Count > 0 ? highlights : ["See full changelog for details"];
        }

        private void ViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/grzybeek/grzyClothTool/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open changelog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAllTools_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GlobalConstants.GRZY_TOOLS_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open website: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenToolUrl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (string.IsNullOrEmpty(mainProjectsFolder))
                {
                    Show("Please configure the main projects folder in settings first.", 
                         "Configuration Required", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(mainProjectsFolder))
                {
                    Show($"Main projects folder does not exist: {mainProjectsFolder}\n\nPlease update it in settings.", 
                         "Folder Not Found", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                bool nameAccepted = false;
                string projectName = string.Empty;

                while (!nameAccepted)
                {
                    var (result, textBoxValue) = Show("Choose a name for your project", 
                                                       "Project Name", 
                                                       CustomMessageBoxButtons.OKCancel, 
                                                       CustomMessageBoxIcon.None, 
                                                       true);

                    if (result != CustomMessageBoxResult.OK || string.IsNullOrWhiteSpace(textBoxValue))
                    {
                        return;
                    }

                    projectName = textBoxValue.Trim();

                    if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        Show("Project name contains invalid characters. Please choose a different name.", 
                             "Invalid Name", 
                             CustomMessageBoxButtons.OKOnly, 
                             CustomMessageBoxIcon.Warning);
                        continue;
                    }

                    var projectFolder = Path.Combine(mainProjectsFolder, projectName);
                    if (Directory.Exists(projectFolder))
                    {
                        var autoSavePath = Path.Combine(projectFolder, "autosave.json");
                        if (File.Exists(autoSavePath))
                        {
                            var openExisting = Show(
                                $"A project with the name '{projectName}' already exists.\n\nDo you want to open it instead?",
                                "Project Exists",
                                CustomMessageBoxButtons.YesNo,
                                CustomMessageBoxIcon.Question);

                            if (openExisting == CustomMessageBoxResult.Yes)
                            {
                                await SaveHelper.LoadSaveFileAsync(autoSavePath);
                                LoadRecentProjects();
                                MainWindow.NavigationHelper.Navigate("Project");
                                return;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            var useFolder = Show(
                                $"A folder named '{projectName}' already exists but contains no save file.\n\nDo you want to create a new project in this folder?",
                                "Folder Exists",
                                CustomMessageBoxButtons.YesNo,
                                CustomMessageBoxIcon.Question);

                            if (useFolder == CustomMessageBoxResult.Yes)
                            {
                                nameAccepted = true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        nameAccepted = true;
                    }
                }

                var finalProjectFolder = Path.Combine(mainProjectsFolder, projectName);
                Directory.CreateDirectory(finalProjectFolder);

                var assetsFolder = Path.Combine(finalProjectFolder, GlobalConstants.ASSETS_FOLDER_NAME);
                Directory.CreateDirectory(assetsFolder);

                MainWindow.AddonManager.ProjectName = projectName;
                MainWindow.AddonManager.CreateAddon();

                var newProjectAutoSavePath = Path.Combine(finalProjectFolder, "autosave.json");
                PersistentSettingsHelper.Instance.AddRecentProject(
                    newProjectAutoSavePath,
                    projectName,
                    drawableCount: 0,
                    addonCount: 1
                );
                
                LoadRecentProjects();

                LogHelper.Log($"Created new project: {projectName} at {finalProjectFolder}");
                MainWindow.NavigationHelper.Navigate("Project");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to create new project: {ex.Message}", Views.LogType.Error);
                Show($"Failed to create new project: {ex.Message}", 
                     "Error", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.OpenAddonAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.ImportProjectAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void OpenSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Save File",
                    Filter = "Save files (*.json)|*.json|All files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(openFileDialog.FileName);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
            }
            catch (Exception ex)
            {
                Show($"Failed to load save: {ex.Message}", 
                     "Error", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void RecentProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Show("This save file no longer exists.", 
                             "File Not Found", 
                             CustomMessageBoxButtons.OKOnly, 
                             CustomMessageBoxIcon.Warning);
                        
                        var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                        recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                        PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                        LoadRecentProjects();
                        return;
                    }

                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(filePath);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
                catch (Exception ex)
                {
                    Show($"Failed to load save: {ex.Message}", 
                         "Error", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Error);
                }
            }
        }

        private void RemoveRecentProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    var project = PersistentSettingsHelper.Instance.RecentlyOpenedProjects
                        .FirstOrDefault(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    
                    var projectName = project?.ProjectName ?? "";
                    
                    var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                    recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                    
                    LoadRecentProjects();
                    
                    LogHelper.Log($"Removed project from recent list: {projectName}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to remove recent project: {ex.Message}", Views.LogType.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ToolInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}
