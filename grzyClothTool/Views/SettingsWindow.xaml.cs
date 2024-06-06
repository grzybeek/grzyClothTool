using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : UserControl
    { 
        public static string GTAVPath => CWHelper.GTAVPath;
        public static bool CacheStartupIsChecked => CWHelper.IsCacheStartupEnabled;

        public static bool DrawablePathIsChecked => Properties.Settings.Default.DisplaySelectedDrawablePath;

        public static bool IsDarkMode => Properties.Settings.Default.IsDarkMode;

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private void GTAVPath_Click(object sender, RoutedEventArgs e)
        {
            //get title from e
            var title = e.Source.GetType().GetProperty("Title").GetValue(e.Source).ToString();

            OpenFolderDialog selectedGTAPath = new()
            {
                Title = title,
                Multiselect = false
            };

            if (selectedGTAPath.ShowDialog() == true)
            {
                var exeFilePath = selectedGTAPath.FolderName + "\\GTA5.exe";
                var isPathValid = File.Exists(exeFilePath);

                if (isPathValid)
                {
                    CWHelper.SetGTAFolder(selectedGTAPath.FolderName);
                }
            }
        }

        private void CacheSettings_Click(object sender, RoutedEventArgs e)
        {
            CheckBoxClickEventArgs c = e as CheckBoxClickEventArgs;
            //CWHelper.SetCacheStartup(c.IsChecked);

            LogHelper.Log($"This is not implemented yet :(", LogType.Warning);
        }

        private void DrawablePathSettings_Click(object sender, RoutedEventArgs e)
        {
            CheckBoxClickEventArgs c = e as CheckBoxClickEventArgs;
            SettingsHelper.Instance.DisplaySelectedDrawablePath = c.IsChecked;
        }

        public void ThemeModeChange_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && toggleButton.IsChecked.HasValue)
            {
                var value = (bool)toggleButton.IsChecked;
                App.ChangeTheme(value);

                Properties.Settings.Default.IsDarkMode = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
