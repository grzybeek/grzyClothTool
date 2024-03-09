using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
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

        private static AddonManager _addon;
        public static AddonManager Addon => _addon;

        public MainWindow()
        {
            App.splashScreen.AddMessage("Loading");
            InitializeComponent();
            CWHelper.Init();

            _instance = this;
            _addon = new AddonManager("name");

            _navigationHelper = new NavigationHelper();
            _navigationHelper.RegisterPage("Project", () => new ProjectWindow());
            _navigationHelper.RegisterPage("Settings", () => new SettingsWindow());

            DataContext = _navigationHelper;
            _navigationHelper.Navigate("Project");

            App.splashScreen.LoadComplete();
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
        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog metaFiles = new()
            {
                Title = "Select .meta file(s)",
                Multiselect = true,
                Filter = "Meta files (*.meta)|*.meta"
            };

            if (metaFiles.ShowDialog() == true)
            {
                //foreach (var fldr in folder.FolderNames)
                //{
                //    foreach (var file in Directory.GetFiles(fldr, "*.ydd", SearchOption.TopDirectoryOnly))
                //    {
                //        Addon.AddDrawable(file, isMaleBtn);
                //    }
                //}

                foreach (var dir in metaFiles.FileNames)
                {
                    _addon.LoadAddon(dir);
                }
            }
        }


        // if main window is closed, close CW window too
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (CWHelper.CWForm.formopen)
            {
                CWHelper.CWForm.Close();
            }
        }
    }
}
