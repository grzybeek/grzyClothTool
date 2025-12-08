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
                    PreviewHost?.InitializePreviewInBackground();
                });
            }));

            this.Loaded += MainWindow_Loaded;
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

        public void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            _ = OpenAddonAsync();
        }

        public async Task OpenAddonAsync(bool shouldSetProjectName = false)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return;
            }

            OpenFileDialog metaFiles = new()
            {
                Title = "Select .meta file(s)",
                Multiselect = true,
                Filter = "Meta files (*.meta)|*.meta"
            };

            if (metaFiles.ShowDialog() == true)
            {
                ProgressHelper.Start("Started loading addon");

                // Opening existing addon, should clear everything and add new opened ones
                AddonManager.Addons = [];
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
                            return;
                        }
                    }

                    await AddonManager.LoadAddon(dir, shouldSetProjectName);
                }

                ProgressHelper.Stop("Addon loaded in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            _ = ImportProjectAsync();
        }

        public async Task ImportProjectAsync(bool shouldSetProjectName = false)
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
                    return;
                }

                foreach (var metaFile in metaFiles)
                {
                    await AddonManager.LoadAddon(metaFile);
                }

                ProgressHelper.Stop("Project imported in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
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
    }
}
