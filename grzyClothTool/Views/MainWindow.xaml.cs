using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using Material.Icons;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using AvalonDock.Themes;
using static grzyClothTool.Enums;

namespace grzyClothTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string AppVersion => "Version: " + UpdateHelper.GetCurrentVersion();
        private static MainWindow _instance;
        public static MainWindow Instance => _instance;
        private static NavigationHelper _navigationHelper;
        public static NavigationHelper NavigationHelper => _navigationHelper;

        private static AddonManager _addonManager;
        public static AddonManager AddonManager => _addonManager;

        private readonly static Dictionary<string, string> TempFoldersNames = new()
        {
            { "import", "grzyClothTool_import" },
            { "export", "grzyClothTool_export" },
            { "dragdrop", "grzyClothTool_dragdrop" }
        };

        public MainWindow()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
            CWHelper.Init();
            _ = TelemetryHelper.LogSession(true);

            _instance = this;
            _addonManager = new AddonManager();
            SaveHelper.AutoSaveProgress += OnAutoSaveProgress;
            SaveHelper.RemainingSecondsChanged += OnRemainingSecondsChanged;

            _navigationHelper = new NavigationHelper();
            _navigationHelper.RegisterPage("Home", () => new Home());
            _navigationHelper.RegisterPage("Project", () => new ProjectWindow());
            _navigationHelper.RegisterPage("Settings", () => new SettingsWindow());

            DataContext = _navigationHelper;
            _navigationHelper.Navigate("Home");

            TempFoldersCleanup();

            FileHelper.GenerateReservedAssets();
            LogHelper.Init();
            LogHelper.LogMessageCreated += LogHelper_LogMessageCreated;
            ProgressHelper.ProgressStatusChanged += ProgressHelper_ProgressStatusChanged;

            SaveHelper.Init();

            CWHelper.DockedPreviewHost = PreviewHost;

            PreviewAnchorable.Closing += PreviewAnchorable_Closing;
            PreviewAnchorable.IsVisibleChanged += PreviewAnchorable_IsVisibleChanged;

            Dispatcher.BeginInvoke((Action)(async () =>
            {
#if !DEBUG
                App.splashScreen.AddMessage("Checking for updates...");
                await UpdateHelper.CheckForUpdates();
#endif
                App.splashScreen.AddMessage("Starting app");

                while (App.splashScreen.MessageQueueCount > 0)
                {
                    await Task.Delay(2000);
                }

                await App.splashScreen.LoadComplete();

                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        PreviewHost?.InitializePreviewInBackground();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogHelper.LogError("Error during 3D preview initialization in MainWindow", ex);
                    }
                });
            }));

            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                await SaveAsync();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PreviewAnchorable.Hide();
            
            bool isDarkMode = Properties.Settings.Default.IsDarkMode;
            App.ChangeTheme(isDarkMode);

            CheckFirstRun();
        }

        private void OnAutoSaveProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                if (percentage > 0 && SaveHelper.HasUnsavedChanges)
                {
                    AutoSaveIndicator.Visibility = Visibility.Visible;
                    AutoSaveIndicator.UpdateProgress(percentage);
                }
                else
                {
                    AutoSaveIndicator.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void OnRemainingSecondsChanged(int seconds)
        {
            Dispatcher.Invoke(() =>
            {
                AutoSaveIndicator.RemainingSeconds = seconds;
            });
        }

        private void CheckFirstRun()
        {
            if (PersistentSettingsHelper.Instance.IsFirstRun)
            {
                this.Hide();
                
                var setupWindow = new FirstRunSetupWindow
                {
                    Owner = null
                };
                
                bool? result = setupWindow.ShowDialog();
                
                if (result == true && setupWindow.SetupCompleted)
                {
                    this.Show();
                    
                    if (!string.IsNullOrEmpty(PersistentSettingsHelper.Instance.MainProjectsFolder))
                    {
                        LogHelper.Log($"Main projects folder set to: {PersistentSettingsHelper.Instance.MainProjectsFolder}", LogType.Info);
                    }
                }
            }
        }
        
        private void PreviewAnchorable_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            PreviewAnchorable.Hide();
            AddonManager.IsPreviewEnabled = false;
        }

        private void PreviewAnchorable_IsVisibleChanged(object sender, EventArgs e)
        {
            if (PreviewAnchorable.IsVisible)
            {
                PreviewHost?.InitializePreview();
            }
        }

        private void ProgressHelper_ProgressStatusChanged(object sender, ProgressMessageEventArgs e)
        {
            var visibility = e.Status switch
            {
                ProgressStatus.Start => Visibility.Visible,
                ProgressStatus.Stop => Visibility.Hidden,
                _ => Visibility.Collapsed
            };

            this.Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = visibility;
            });
        }

        private void LogHelper_LogMessageCreated(object sender, LogMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                logBar.Text = e.Message;
                logBarIcon.Kind = Enum.Parse<MaterialIconKind>(e.TypeIcon);
                logBarIcon.Visibility = Visibility.Visible;
            });
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = e.Uri.AbsoluteUri;
            p.Start();
        }

        //this is needed so window can be clicked anywhere to unfocus textbox
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void Navigation_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement).Tag.ToString();

            _navigationHelper.Navigate(tag);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            if (!SaveHelper.HasUnsavedChanges)
            {
                LogHelper.Log("No changes to save", LogType.Info);
                return;
            }

            await SaveHelper.SaveAsync();
        }

        public void HomeScreen_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return;
            }

            AddonManager.Addons.Clear();
            AddonManager.ProjectName = string.Empty;
            AddonManager.IsExternalProject = false;
            AddonManager.Groups.Clear();
            AddonManager.Tags.Clear();
            AddonManager.MoveMenuItems.Clear();
            AddonManager.SelectedAddon = null;
            AddonManager.IsPreviewEnabled = false;

            DuplicateDetector.Clear();

            SaveHelper.SetUnsavedChanges(false);

            _navigationHelper.Navigate("Home");

            LogHelper.Log("Cleared data, moved to home screen", LogType.Info);
        }

        public void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            _ = OpenAddonAsync();
        }

        public void AddAddon_Click(object sender, RoutedEventArgs e)
        {
            _ = AddAddonAsync();
        }

        public async Task<bool> OpenAddonAsync(bool shouldSetProjectName = false)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return false;
            }

            OpenFileDialog metaFiles = new()
            {
                Title = "Select .meta file(s)",
                Multiselect = true,
                Filter = "Meta files (*.meta)|*.meta"
            };

            if (metaFiles.ShowDialog() != true)
            {
                return false;
            }

            var validMetaFiles = new List<string>();
            var extractedProjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalDrawableCount = 0;
            string suggestedProjectName = string.Empty;

            foreach (var dir in metaFiles.FileNames)
            {
                using (var reader = new StreamReader(dir))
                {
                    string firstLine = await reader.ReadLineAsync();
                    string secondLine = await reader.ReadLineAsync();

                    if ((firstLine == null || !firstLine.Contains("ShopPedApparel")) &&
                        (secondLine == null || !secondLine.Contains("ShopPedApparel")))
                    {
                        LogHelper.Log($"Skipped file {dir} as it is probably not a correct .meta file");
                        continue;
                    }
                }

                validMetaFiles.Add(dir);

                var addonName = Path.GetFileNameWithoutExtension(dir);
                string extractedName;
                
                if (addonName.StartsWith("mp_m_freemode_01_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_m_freemode_01_".Length..];
                }
                else if (addonName.StartsWith("mp_f_freemode_01_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_f_freemode_01_".Length..];
                }
                else if (addonName.StartsWith("mp_m_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_m_".Length..];
                }
                else if (addonName.StartsWith("mp_f_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_f_".Length..];
                }
                else
                {
                    extractedName = addonName;
                }
                
                extractedProjectNames.Add(extractedName);

                var dirPath = Path.GetDirectoryName(dir);
                var addonFileName = Path.GetFileNameWithoutExtension(dir);
                string genderPart = addonFileName.Contains("mp_m_freemode_01") ? "mp_m_freemode_01" : "mp_f_freemode_01";
                string addonNameWithoutGender = addonFileName.Replace(genderPart, "").TrimStart('_');
                
                var yddFiles = await Task.Run(() =>
                {
                    string pattern = $@"^{genderPart}(_p)?.*?{System.Text.RegularExpressions.Regex.Escape(addonNameWithoutGender)}\^";
                    var compiledPattern = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
                    
                    return Directory.GetFiles(dirPath, "*.ydd", SearchOption.AllDirectories)
                        .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                        .Count();
                });
                
                totalDrawableCount += yddFiles;
            }

            if (validMetaFiles.Count == 0)
            {
                Controls.CustomMessageBox.Show("No valid .meta files were selected.", "Error", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                return false;
            }

            if (extractedProjectNames.Count == 1)
            {
                suggestedProjectName = extractedProjectNames.First();
            }
            else if (extractedProjectNames.Count > 1)
            {
                var names = extractedProjectNames.ToList();
                var commonPrefix = FindCommonPrefix(names);
                
                if (!string.IsNullOrEmpty(commonPrefix) && commonPrefix.Length >= 3)
                {
                    suggestedProjectName = commonPrefix.TrimEnd('_');
                }
                else
                {
                    suggestedProjectName = names.First();
                }
            }

            if (totalDrawableCount == 0)
            {
                Controls.CustomMessageBox.Show(
                    "No drawable files (.ydd) were found for the selected .meta file(s).\n\n" +
                    "Please make sure the .ydd files are in the same directory or subdirectories as the .meta file.",
                    "No Drawables Found", 
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            var dialog = ProjectSetupDialog.ShowForOpenAddon(this, suggestedProjectName, totalDrawableCount, validMetaFiles.Count);
            if (!dialog.Confirmed)
            {
                return false;
            }

            ProgressHelper.Start("Started loading addon");

            try
            {
                AddonManager.Addons = [];
                AddonManager.IsExternalProject = !dialog.IsSelfContained;
                DuplicateDetector.Clear();

                foreach (var metaFile in validMetaFiles)
                {
                    await AddonManager.LoadAddon(metaFile, shouldSetProjectName);
                }

                AddonManager.ProjectName = dialog.ProjectName;

                var projectType = dialog.IsSelfContained ? "Self-contained" : "External";
                ProgressHelper.Stop($"{projectType} addon loaded in {{0}}", true);
                SaveHelper.SetUnsavedChanges(true);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to load addon: {ex.Message}", Views.LogType.Error);
                ProgressHelper.Stop("Failed to load addon", false);
                return false;
            }
        }

        public async Task AddAddonAsync(bool shouldSetProjectName = false)
        {
            OpenFileDialog metaFiles = new()
            {
                Title = "Select .meta file(s) to add",
                Multiselect = true,
                Filter = "Meta files (*.meta)|*.meta"
            };

            if (metaFiles.ShowDialog() == true)
            {
                ProgressHelper.Start("Started adding addon");

                try
                {
                    // Import addon - add to existing addons instead of replacing
                    foreach (var dir in metaFiles.FileNames)
                    {
                        using (var reader = new StreamReader(dir))
                        {
                            string firstLine = await reader.ReadLineAsync();
                            string secondLine = await reader.ReadLineAsync();

                            //Check two first lines if it contains "ShopPedApparel"
                            if ((firstLine == null || !firstLine.Contains("ShopPedApparel")) &&
                                (secondLine == null || !secondLine.Contains("ShopPedApparel")))
                            {
                                LogHelper.Log($"Skipped file {dir} as it is probably not a correct .meta file");
                                continue;
                            }
                        }

                        await AddonManager.LoadAddon(dir, shouldSetProjectName);
                    }

                    ProgressHelper.Stop("Addon added in {0}", true);
                    SaveHelper.SetUnsavedChanges(true);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to add addon: {ex.Message}", Views.LogType.Error);
                    ProgressHelper.Stop("Failed to add addon", false);
                }
            }
        }

        private void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            _ = ImportProjectAsync();
        }

        public async Task<bool> ImportProjectAsync(bool shouldSetProjectName = false)
        {

            // open file dialog to select project file
            OpenFileDialog openFileDialog = new()
            {
                Title = "Import project",
                Filter = "grzyClothTool project (*.gctproject)|*.gctproject"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProgressHelper.Start($"Started importing {openFileDialog.SafeFileName}");

                try
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), TempFoldersNames["import"]);
                    Directory.CreateDirectory(tempPath);

                    var selectedPath = openFileDialog.FileName;
                    var projectName = Path.GetFileNameWithoutExtension(selectedPath);

                    if (shouldSetProjectName)
                    {
                        AddonManager.ProjectName = projectName;
                    }

                    var buildPath = Path.Combine(tempPath, projectName + "_" + DateTime.UtcNow.Ticks.ToString());

                    var zipPath = Path.Combine(tempPath, $"{projectName}.zip");

                    await ObfuscationHelper.XORFile(selectedPath, zipPath);
                    await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, buildPath));

                    if (File.Exists(zipPath))
                    {
                        //delete zip after extract
                        File.Delete(zipPath);
                    }

                    var metaFiles = Directory.GetFiles(buildPath, "*.meta", SearchOption.TopDirectoryOnly)
                             .Where(file => file.Contains("mp_m_freemode") || file.Contains("mp_f_freemode"))
                             .ToList();

                    if (metaFiles.Count == 0)
                    {
                        LogHelper.Log("No meta files found in project file, this shouldn't happen, please report it to developer on discord");
                        ProgressHelper.Stop("Project import failed", false);
                        return false;
                    }

                    foreach (var metaFile in metaFiles)
                    {
                        await AddonManager.LoadAddon(metaFile);
                    }

                    ProgressHelper.Stop("Project imported in {0}", true);
                    SaveHelper.SetUnsavedChanges(true);
                    return true;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to import project: {ex.Message}", Views.LogType.Error);
                    ProgressHelper.Stop("Failed to import project", false);
                    return false;
                }
            }

            return false;
        }

        private async void ExportProject_Click(object sender, RoutedEventArgs e)
        {
            // As export we can build current project as a fivem resource, because fivem is most common and it's easy to load it later
            // Then zip it and save it as a project file

            var savedProjectName = string.IsNullOrWhiteSpace(AddonManager.ProjectName) ? "project" : AddonManager.ProjectName;
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Export project",
                Filter = "grzyClothTool project (*.gctproject)|*.gctproject",
                FileName = $"{savedProjectName}.gctproject"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ProgressHelper.Start("Started exporting project");

                try
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), TempFoldersNames["export"]);

                    var selectedPath = saveFileDialog.FileName;
                    var projectName = Path.GetFileNameWithoutExtension(selectedPath);
                    var buildPath = Path.Combine(tempPath, projectName);

                    var bHelper = new BuildResourceHelper(projectName, buildPath, new Progress<int>(), BuildResourceType.FiveM, false);
                    await bHelper.BuildFiveMResource();

                    var zipPath = Path.Combine(tempPath, $"{projectName}.zip");

                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }
                    if (File.Exists(selectedPath))
                    {
                        File.Delete(selectedPath);
                    }

                    await Task.Run(() => ZipFile.CreateFromDirectory(buildPath, zipPath, CompressionLevel.Fastest, false));
                    await ObfuscationHelper.XORFile(zipPath, selectedPath);

                    ProgressHelper.Stop("Project exported in {0}", true);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to export project: {ex.Message}", Views.LogType.Error);
                    ProgressHelper.Stop("Failed to export project", false);
                }
            }
        }

        // if main window is closed, close CW window too
        private void Window_Closed(object sender, System.EventArgs e)
        {
            _ = TelemetryHelper.LogSession(false);

            PreviewHost?.ClosePreview();
            LogHelper.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                e.Cancel = true;
            }
        }

        private void StatusBarItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LogHelper.OpenLogWindow();
        }

        private void LogsOpen_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.OpenLogWindow();
        }

        private void DuplicateInspector_Click(object sender, RoutedEventArgs e)
        {
            var inspector = new DuplicateInspectorWindow();
            inspector.ShowDialog();
        }

        private static void TempFoldersCleanup()
        {
            // At the start of app we can remove temp folders from previous session

            foreach (var tempName in TempFoldersNames.Values)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), tempName);
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }

        public void UpdateAvalonDockTheme(bool isDarkMode)
        {
            if (DockManager != null)
            {
                DockManager.Theme = isDarkMode ? new Vs2013DarkTheme() : new Vs2013LightTheme();
            }
        }

        private static string FindCommonPrefix(List<string> strings)
        {
            if (strings == null || strings.Count == 0)
                return string.Empty;

            if (strings.Count == 1)
                return strings[0];

            var prefix = strings[0];
            
            for (int i = 1; i < strings.Count; i++)
            {
                while (!strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = prefix[..^1];
                    if (string.IsNullOrEmpty(prefix))
                        return string.Empty;
                }
            }

            return prefix;
        }
    }
}
