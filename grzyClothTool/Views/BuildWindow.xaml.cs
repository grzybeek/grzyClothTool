using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for BuildWindow.xaml
    /// </summary>
    public partial class BuildWindow : Window, INotifyPropertyChanged
    {
        public enum ResourceType
        {
            FiveM,
            AltV,
            Singleplayer
        }

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
        public string BuildPath { get; set; }

        private ResourceType _resourceType;

        public BuildWindow()
        {
            InitializeComponent();
            DataContext = this;
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
                case ResourceType.FiveM:
                    await BuildFiveMResource(buildHelper);
                    break;
                case ResourceType.AltV:
                    await BuildAltVResource(buildHelper);
                    break;
                case ResourceType.Singleplayer:
                    await BuildSingleplayerResource(buildHelper);
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
                var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, MainWindow.AddonManager.Addons.Count, progress);

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

        private void AddBuildTasksForSex(BuildResourceHelper bHelper, Addon selectedAddon, Enums.SexType sexType, List<Task> tasks, List<string> metaFiles, int counter, RpfDirectoryEntry cdimages = null, RpfDirectoryEntry dataFolder = null)
        {
            if (selectedAddon.HasSex(sexType))
            {
                var bytes = bHelper.BuildYMT(sexType);
                if (_resourceType == ResourceType.FiveM)
                {
                    tasks.Add(bHelper.BuildFiveMFilesAsync(sexType, bytes, counter));
                }
                else if (_resourceType == ResourceType.AltV)
                {
                    tasks.Add(bHelper.BuildAltVFilesAsync(sexType, bytes, counter));
                }
                else if (_resourceType == ResourceType.Singleplayer)
                {
                    var (name, metaBytes) = bHelper.BuildMeta(sexType);
                    RpfFile.CreateFile(dataFolder, name, metaBytes);
                    tasks.Add(bHelper.BuildSingleplayerFilesAsync(sexType, bytes, counter, cdimages));
                }

                if (_resourceType != ResourceType.Singleplayer)
                {
                    var (metaName, metaContent) = bHelper.BuildMeta(sexType);
                    metaFiles.Add(metaName);

                    var path = _resourceType == ResourceType.FiveM ? Path.Combine(BuildPath, metaName) : Path.Combine(BuildPath, "stream", metaName);
                    tasks.Add(File.WriteAllBytesAsync(path, metaContent));
                }
            }
        }

        private async Task BuildFiveMResource(BuildResourceHelper bHelper)
        {
            int counter = 1;
            var metaFiles = new List<string>();
            var tasks = new List<Task>();

            foreach (var selectedAddon in MainWindow.AddonManager.Addons)
            {
                bHelper.SetAddon(selectedAddon);
                bHelper.SetNumber(counter);

                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.male, tasks, metaFiles, counter);
                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.female, tasks, metaFiles, counter);

                counter++;
            }

            await Task.WhenAll(tasks);
            bHelper.BuildFirstPersonAlternatesMeta();
            bHelper.BuildFxManifest(metaFiles);
        }

        private async Task BuildAltVResource(BuildResourceHelper bHelper)
        {
            int counter = 1;
            var metaFiles = new List<string>();
            var tasks = new List<Task>();

            foreach (var selectedAddon in MainWindow.AddonManager.Addons)
            {
                bHelper.SetAddon(selectedAddon);
                bHelper.SetNumber(counter);

                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.male, tasks, metaFiles, counter);
                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.female, tasks, metaFiles, counter);

                counter++;
            }

            await Task.WhenAll(tasks);
            bHelper.BuildAltVTomls(metaFiles);
        }

        private async Task BuildSingleplayerResource(BuildResourceHelper bHelper)
        {
            var dlcRpf = RpfFile.CreateNew(BuildPath, "dlc.rpf", RpfEncryption.OPEN);
            var creatureMetadatas = bHelper.BuildContentXml(dlcRpf.Root);
            bHelper.BuildSetupXml(dlcRpf.Root);

            var x64 = RpfFile.CreateDirectory(dlcRpf.Root, "x64");
            var common = RpfFile.CreateDirectory(dlcRpf.Root, "common");
            var dataFolder = RpfFile.CreateDirectory(common, "data");

            var models = RpfFile.CreateDirectory(x64, "models");
            var cdimages = RpfFile.CreateDirectory(models, "cdimages");

            if (creatureMetadatas.Count > 0)
            {
                var animFolder = RpfFile.CreateDirectory(x64, "anim");
                var creature = RpfFile.CreateNew(animFolder, "creaturemetadata.rpf");

                foreach (var meta in creatureMetadatas)
                {
                    RpfFile.CreateFile(creature.Root, meta.SingleplayerFileName + ".ymt", meta.Save());
                }
            }

            int counter = 1;
            var tasks = new List<Task>();
            var metaFiles = new List<string>();

            foreach (var selectedAddon in MainWindow.AddonManager.Addons)
            {
                bHelper.SetAddon(selectedAddon);
                bHelper.SetNumber(counter);

                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.male, tasks, metaFiles, counter, cdimages, dataFolder);
                AddBuildTasksForSex(bHelper, selectedAddon, Enums.SexType.female, tasks, metaFiles, counter, cdimages, dataFolder);

                counter++;
            }

            await Task.WhenAll(tasks);
        }

        private void RadioButton_ChangedEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ModernLabelRadioButton radioButton && radioButton.IsChecked == true)
            {
                _resourceType = radioButton.Label switch
                {
                    "FiveM" => ResourceType.FiveM,
                    "AltV" => ResourceType.AltV,
                    "Singleplayer" => ResourceType.Singleplayer,
                    _ => throw new NotImplementedException()
                };
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
