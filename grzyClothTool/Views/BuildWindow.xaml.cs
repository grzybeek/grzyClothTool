using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public string ProjectName { get; set; } = MainWindow.AddonManager.ProjectName;

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

        private bool _isWarningVisible;
        public bool IsWarningVisible
        {
            get => _isWarningVisible;
            set
            {
                if (_isWarningVisible != value)
                {
                    _isWarningVisible = value;
                    OnPropertyChanged(nameof(IsWarningVisible));
                }
            }
        }

        private string _warningMessage;
        public string WarningMessage
        {
            get => _warningMessage;
            set
            {
                if (_warningMessage != value)
                {
                    _warningMessage = value;
                    OnPropertyChanged(nameof(WarningMessage));
                }
            }
        }

        private bool _canBuild = true;
        public bool CanBuild
        {
            get => _canBuild;
            set
            {
                if (_canBuild != value)
                {
                    _canBuild = value;
                    OnPropertyChanged(nameof(CanBuild));
                }
            }
        }

        public string BuildPath { get; set; } = GetDefaultBuildPath();

        private static string GetDefaultBuildPath()
        {
            var projectName = MainWindow.AddonManager.ProjectName;
            var mainFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;

            if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(mainFolder))
            {
                return string.Empty;
            }

            return Path.Combine(mainFolder, projectName, "build_output");
        }

        private BuildResourceType _resourceType;

        public BuildWindow()
        {
            InitializeComponent();
            DataContext = this;

            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;

            CheckAddons();
        }

        private void CheckAddons()
        {
            if (string.IsNullOrEmpty(ProjectName))
            {
                IsWarningVisible = true;
                WarningMessage = "No project loaded. Please create or open a project first.";
                CanBuild = false;
                return;
            }

            if (string.IsNullOrEmpty(BuildPath))
            {
                IsWarningVisible = true;
                WarningMessage = "Build path could not be determined. Please check your project settings.";
                CanBuild = false;
                return;
            }

            var allDrawablesCount = MainWindow.AddonManager.Addons.Sum(a => a.Drawables.Count);
            if (allDrawablesCount == 0)
            {
                IsWarningVisible = true;
                WarningMessage = "No drawables found. Add drawables to be able to build resource.";
                CanBuild = false;
            }
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

            if (string.IsNullOrEmpty(ProjectName) || string.IsNullOrEmpty(BuildPath))
            {
                CustomMessageBox.Show("Please fill in all fields. Make sure a project is loaded.", "Error", CustomMessageBoxButtons.OKOnly);
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

            await SaveHelper.SaveAsync();

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
                LogHelper.Log($"Build failed: {ex}", LogType.Error);
                CustomMessageBox.Show($"Build failed:\n\n{ex}", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
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
