using grzyClothTool.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for FirstRunSetupWindow.xaml
    /// </summary>
    public partial class FirstRunSetupWindow : Window
    {
        public bool SetupCompleted { get; private set; }

        public FirstRunSetupWindow()
        {
            InitializeComponent();
            
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolder = Path.Combine(documentsPath, "grzyClothTool Projects");
            FolderPathTextBox.Text = defaultFolder;
            ContinueButton.IsEnabled = true;

            Closing += FirstRunSetupWindow_Closing;
        }

        private void FirstRunSetupWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!SetupCompleted)
            {
                var result = System.Windows.MessageBox.Show(
                    "You must select a main folder to continue using the application.\n\nThis will close the application. Are you sure?",
                    "Setup Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    // User wants to exit the application entirely
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the MAIN folder where ALL your projects will be stored (not a specific project folder)";
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrWhiteSpace(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                dialog.SelectedPath = FolderPathTextBox.Text;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
                ValidationMessage.Visibility = Visibility.Collapsed;
                ContinueButton.IsEnabled = true;
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPath = FolderPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                ValidationMessage.Text = "Please select a main folder before continuing.";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            if (PersistentSettingsHelper.IsRootDrive(selectedPath))
            {
                ValidationMessage.Text = "You cannot use a root drive (e.g., C:\\) as the main folder. Please select or create a subfolder.";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (!Directory.Exists(selectedPath))
                {
                    Directory.CreateDirectory(selectedPath);
                }

                string testFile = Path.Combine(selectedPath, ".grzyClothTool_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                PersistentSettingsHelper.Instance.MainProjectsFolder = selectedPath;
                PersistentSettingsHelper.Instance.IsFirstRun = false;

                SetupCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (UnauthorizedAccessException)
            {
                ValidationMessage.Text = "Access denied. Please select a folder where you have write permissions.";
                ValidationMessage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ValidationMessage.Text = $"Error: {ex.Message}";
                ValidationMessage.Visibility = Visibility.Visible;
            }
        }
    }
}
