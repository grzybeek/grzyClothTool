using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for BuildWindow.xaml
    /// </summary>
    public partial class BuildWindow : Window
    {
        public enum ResourceType
        {
            FiveM,
            Singleplayer
        }

        public string ProjectName { get; set; }
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

        private async void build_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            if (ProjectName == null || BuildPath == null)
            {
                MessageBox.Show("Please fill in all fields");

                CustomMessageBox.Show($"Please fill in all fields", "Build done", CustomMessageBoxButtons.OKCancel);
                return;
            }
            var timer = new Stopwatch();
            timer.Start();
            var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, MainWindow.AddonManager.Addons.Count);

            switch (_resourceType)
            {
                case ResourceType.FiveM:
                    await BuildFiveMResource(buildHelper);
                    break;
                case ResourceType.Singleplayer:
                    await buildHelper.BuildSingleplayer();
                    break;
                default:
                    throw new NotImplementedException();
            }

            Close();
            timer.Stop();

            CustomMessageBox.Show($"Build done, elapsed time: {timer.Elapsed}", "Build done", CustomMessageBoxButtons.OpenFolder, BuildPath);
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

                if (selectedAddon.HasMale)
                {
                    var bytes = bHelper.BuildYMT(true);
                    tasks.Add(bHelper.BuildFiveMFilesAsync(true, bytes, counter));

                    var (name, b) = bHelper.BuildMeta(true);
                    metaFiles.Add(name);

                    var path = Path.Combine(BuildPath, name);
                    tasks.Add(File.WriteAllBytesAsync(path, b));
                }
                if (selectedAddon.HasFemale)
                {
                    var bytes = bHelper.BuildYMT(false);
                    tasks.Add(bHelper.BuildFiveMFilesAsync(false, bytes, counter));

                    var (name, b) = bHelper.BuildMeta(false);
                    metaFiles.Add(name);

                    var path = Path.Combine(BuildPath, name);
                    tasks.Add(File.WriteAllBytesAsync(path, b));
                }
                counter++;
            }
            await Task.WhenAll(tasks);
            bHelper.BuildFxManifest(metaFiles);
        }

        private void RadioButton_ChangedEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ModernLabelRadioButton radioButton && radioButton.IsChecked == true)
            {
                _resourceType = radioButton.Label switch
                {
                    "FiveM" => ResourceType.FiveM,
                    "Singleplayer" => ResourceType.Singleplayer,
                    _ => throw new NotImplementedException()
                };
            }
        }
    }
}
