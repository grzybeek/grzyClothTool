using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using Material.Icons;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using static grzyClothTool.Enums;

namespace grzyClothTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow _instance;
        public static MainWindow Instance => _instance;
        private static NavigationHelper _navigationHelper;
        public static NavigationHelper NavigationHelper => _navigationHelper;

        private static AddonManager _addonManager;
        public static AddonManager AddonManager => _addonManager;

        public MainWindow()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
            CWHelper.Init();

            _instance = this;
            _addonManager = new AddonManager();
            _addonManager.CreateAddon();

            _navigationHelper = new NavigationHelper();
            _navigationHelper.RegisterPage("Project", () => new ProjectWindow());
            _navigationHelper.RegisterPage("Settings", () => new SettingsWindow());

            DataContext = _navigationHelper;
            _navigationHelper.Navigate("Project");
            version.Header = "Version: " + UpdateHelper.GetCurrentVersion();

            FileHelper.GenerateReservedAssets();
            LogHelper.Init();
            LogHelper.LogMessageCreated += LogHelper_LogMessageCreated;

            SaveHelper.Init();

            Dispatcher.BeginInvoke((Action)(async () =>
            {
#if !DEBUG
                App.splashScreen.AddMessage("Checking for updates...");
                await UpdateHelper.CheckForUpdates();
#endif
                App.splashScreen.AddMessage("Starting app");

                // Wait until the SplashScreen's message queue is empty
                while (App.splashScreen.MessageQueueCount > 0)
                {
                    await Task.Delay(2000);
                }

                await App.splashScreen.LoadComplete();
            }));
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

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
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
                SaveHelper.SavingPaused = true;
                var timer = new Stopwatch();
                timer.Start();

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

                    await AddonManager.LoadAddon(dir);
                }

                timer.Stop();
                LogHelper.Log($"Loaded addon in {timer.Elapsed}");
                SaveHelper.SetUnsavedChanges(true);
                SaveHelper.SavingPaused = false;
            }
        }

        private async void OpenSave_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is SaveFile saveFile)
            {
                if(!SaveHelper.CheckUnsavedChangesMessage())
                {
                    return;
                }

                await SaveHelper.LoadAsync(saveFile);
            }
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {

            // open file dialog to select project file
            OpenFileDialog openFileDialog = new()
            {
                Title = "Import project",
                Filter = "grzyClothTool project (*.gctproject)|*.gctproject"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SaveHelper.SavingPaused = true;
                var timer = new Stopwatch();
                timer.Start();

                var tempPath = Path.Combine(Path.GetTempPath(), "grzyClothTool_import");
                //we cannot remove this temp folder, because we need it to extract files, and we need them later to build resource
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                var selectedPath = openFileDialog.FileName;
                var projectName = Path.GetFileNameWithoutExtension(selectedPath);
                var buildPath = Path.Combine(tempPath, projectName + "_" + DateTime.UtcNow.Ticks.ToString());

                var zipPath = Path.Combine(tempPath, $"{projectName}.zip");
                ObfuscationHelper.XORFile(selectedPath, zipPath);
                ZipFile.ExtractToDirectory(zipPath, buildPath);

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

                timer.Stop();
                LogHelper.Log($"Project imported in {timer.Elapsed}");
                SaveHelper.SetUnsavedChanges(true);
                SaveHelper.SavingPaused = false;
            }
        }

        private async void ExportProject_Click(object sender, RoutedEventArgs e)
        {
            // As export we can build current project as a fivem resource, because fivem is most common and it's easy to load it later
            // Then zip it and save it as a project file

            SaveFileDialog saveFileDialog = new()
            {
                Title = "Export project",
                Filter = "grzyClothTool project (*.gctproject)|*.gctproject",
                FileName = "project.gctproject"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveHelper.SavingPaused = true;
                var timer = new Stopwatch();
                timer.Start();

                var tempPath = Path.Combine(Path.GetTempPath(), "grzyClothTool_export");
                // make sure there is no temp folder
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                var selectedPath = saveFileDialog.FileName;
                var projectName = Path.GetFileNameWithoutExtension(selectedPath);
                var buildPath = Path.Combine(tempPath, projectName);

                var bHelper = new BuildResourceHelper(projectName, buildPath, new Progress<int>(), BuildResourceType.FiveM);

                await Task.Run(() => bHelper.BuildFiveMResource());

                var zipPath = Path.Combine(tempPath, $"{projectName}.zip");
                ZipFile.CreateFromDirectory(buildPath, zipPath);

                ObfuscationHelper.XORFile(zipPath, selectedPath);

                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }

                timer.Stop();
                LogHelper.Log($"Project exported in {timer.Elapsed}");
                SaveHelper.SavingPaused = false;
            }
        }

        // if main window is closed, close CW window too
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (CWHelper.CWForm.formopen)
            {
                CWHelper.CWForm.Close();
            }

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
    }
}
