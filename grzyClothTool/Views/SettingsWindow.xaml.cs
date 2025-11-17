using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : UserControl, INotifyPropertyChanged
    { 
        public static string GTAVPath => CWHelper.GTAVPath;
        public static bool CacheStartupIsChecked => CWHelper.IsCacheStartupEnabled;

        public static bool IsDarkMode => Properties.Settings.Default.IsDarkMode;

        private string _selectedGroup;
        public string SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.AddonManager.Addons.Count > 0)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
            else
            {
                MainWindow.NavigationHelper.Navigate("Home");
            }
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

        public void PatreonAccount_Click(object sender, RoutedEventArgs e)
        {
            var accountsWindow = new AccountsWindow();
            accountsWindow.ShowDialog();
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

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupPath = NewGroupTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(groupPath))
            {
                CustomMessageBox.Show("Please enter a group name", "Add Group", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (groupPath.Any(c => invalidChars.Contains(c) && c != '/' && c != '\\'))
            {
                CustomMessageBox.Show("Group name contains invalid characters", "Add Group", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Error);
                return;
            }

            GroupManager.Instance.AddGroup(groupPath);
            NewGroupTextBox.Clear();
            LogHelper.Log($"Added group: {groupPath}", LogType.Info);
        }

        private void RemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedGroup))
            {
                CustomMessageBox.Show("Please select a group to remove", "Remove Group", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            var affectedDrawables = MainWindow.AddonManager?.Addons?
                .SelectMany(a => a.Drawables)
                .Where(d => d.Group == SelectedGroup)
                .ToList() ?? [];

            if (affectedDrawables.Count > 0)
            {
                var result = CustomMessageBox.Show(
                    $"The group '{SelectedGroup}' is currently being used by {affectedDrawables.Count} drawable(s).\n\nRemoving it will clear the group from those drawables.\n\nDo you want to continue?",
                    "Group In Use",
                    CustomMessageBox.CustomMessageBoxButtons.OKCancel,
                    CustomMessageBox.CustomMessageBoxIcon.Warning);

                if (result != CustomMessageBox.CustomMessageBoxResult.OK)
                {
                    return;
                }

                foreach (var drawable in affectedDrawables)
                {
                    drawable.Group = null;
                }
            }

            LogHelper.Log($"Removed group: {SelectedGroup}", LogType.Info);
            GroupManager.Instance.RemoveGroup(SelectedGroup);
            SelectedGroup = null;
        }
    }
}
