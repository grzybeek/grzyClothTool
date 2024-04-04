using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : UserControl
    {
        public static string GTAVPath => CWHelper.GTAVPath;
        public static bool CacheStartupIsChecked => CWHelper.IsCacheStartupEnabled;

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

        private void SettingsLabelCheckBox_CheckBoxClick(object sender, RoutedEventArgs e)
        {
            CheckBoxClickEventArgs c = e as CheckBoxClickEventArgs;
            CWHelper.SetCacheStartup(c.IsChecked);

            LogHelper.Log($"This is not implemented yet :(", LogType.Warning);
        }
    }
}
