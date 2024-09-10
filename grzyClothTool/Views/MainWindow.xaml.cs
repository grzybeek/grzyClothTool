using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using Material.Icons;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

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
                AddonManager.ResetDuplicateSearch();
                foreach (var dir in metaFiles.FileNames)
                {
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
