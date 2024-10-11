using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static grzyClothTool.Controls.CustomMessageBox;
using static grzyClothTool.Enums;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for BuildWindow.xaml
    /// </summary>
    public partial class BuildWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ProjectName { get; set; }

        private bool _isBuilding;
        public bool IsBuilding
        {
            get => _isBuilding;
            set
            {
                if (_isBuilding != value)
                {
                    _isBuilding = value;
                    OnPropertyChanged(nameof(IsBuilding));
                }
            }
        }
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        private bool _splitAddons;
        public bool SplitAddons
        {
            get => _splitAddons;
            set
            {
                if (_splitAddons != value)
                {
                    _splitAddons = value;
                    OnPropertyChanged(nameof(SplitAddons));
                }
            }
        }
        public string BuildPath { get; set; }

        private BuildResourceType _resourceType;

        public BuildWindow()
        {
            InitializeComponent();
            DataContext = this;

            // check if there are more than one addon, if not disable split addons
            // maybe this this could be handled by a separate variable or in XAML (?) - not sure
            split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = IsBuilding;
        }

        private async Task BuildResource(BuildResourceHelper buildHelper)
        {
            switch (_resourceType)
            {
                case BuildResourceType.FiveM:
                    await buildHelper.BuildFiveMResource();
                    break;
                case BuildResourceType.AltV:
                    await buildHelper.BuildAltVResource();
                    break;
                case BuildResourceType.Singleplayer:
                    await buildHelper.BuildSingleplayerResource();
                    break;
                default:
                    throw new NotImplementedException($"Unsupported resource type: {_resourceType}");
            }
        }

        private async void build_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            var error = ValidateProjectName();
            if (error != null)
            {
                MessageBox.Show(error);
                return;
            }

            if (ProjectName == null || BuildPath == null)
            {
                MessageBox.Show("Please fill in all fields");
                CustomMessageBox.Show($"Please fill in all fields", "Error", CustomMessageBoxButtons.OKCancel);
                return;
            }

            var buildButton = sender as CustomButton;
            if (buildButton != null)
            {
                buildButton.IsEnabled = false; // blocking interactions - spamming button led to building multiple times/exception
            }

            int totalSteps = MainWindow.AddonManager.GetTotalDrawableAndTextureCount();

            ProgressValue = 0;
            pbBuild.Maximum = totalSteps;
            IsBuilding = true;

            try
            {
                var timer = new Stopwatch();

                timer.Start();

                var progress = new Progress<int>(value => ProgressValue += value);
                var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, progress, _resourceType, SplitAddons);

                await Task.Run(() => BuildResource(buildHelper)); // moved out of ui thread, so users don't think tool stopped responding

                timer.Stop();
                CustomMessageBox.Show($"Build done, elapsed time: {timer.Elapsed}", "Build done", CustomMessageBoxButtons.OpenFolder, BuildPath);
                LogHelper.Log($"Build done, elapsed time: {timer.Elapsed}");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Build failed: {ex.Message}");
                CustomMessageBox.Show($"Build failed: {ex.Message}", "Error");
            }
            finally
            {
                ProgressValue = totalSteps; // make sure that progress bar is full

                if (buildButton != null)
                {
                    buildButton.IsEnabled = true;
                }

                IsBuilding = false;
                Close();
            }
        }

        private void RadioButton_ChangedEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ModernLabelRadioButton radioButton && radioButton.IsChecked == true)
            {
                _resourceType = radioButton.Label switch
                {
                    "FiveM" => BuildResourceType.FiveM,
                    "AltV" => BuildResourceType.AltV,
                    "Singleplayer" => BuildResourceType.Singleplayer,
                    _ => throw new NotImplementedException()
                };


                // Singleplayer doesn't support splitting addons
                if (_resourceType == BuildResourceType.Singleplayer)
                {
                    SplitAddons = false;
                    split_addons.IsEnabled = false;
                }
                else if (DataContext != null) // check if DataContext exist, to prevent error (happens on initialization)
                {
                    split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;
                }
            }
        }

        private string ValidateProjectName()
        {
            string result = null;

            if (string.IsNullOrEmpty(ProjectName))
            {
                result = "Project name cannot be empty";
            }
            else if (ProjectName.Length < 3)
            {
                result = "Project name must be at least 3 characters long";
            }
            else if (ProjectName.Length > 50)
            {
                result = "Project name cannot be longer than 50 characters";
            }
            else if (!Regex.IsMatch(ProjectName, @"^[a-z0-9_]+$"))
            {
                result = "Project name can only contain lowercase letters, numbers, and underscores";

            }

            return result;
        }
    }
}
