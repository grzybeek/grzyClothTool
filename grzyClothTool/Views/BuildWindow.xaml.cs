using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string ProjectName { get; set; }
        public string BuildPath { get; set; }

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

            int counter = 1;
            var metaFiles = new List<string>();
            var tasks = new List<Task>();
            var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, MainWindow.AddonManager.Addons.Count);
            foreach (var selectedAddon in MainWindow.AddonManager.Addons)
            {
                buildHelper.SetAddon(selectedAddon);
                buildHelper.SetNumber(counter);

                if (selectedAddon.HasMale)
                {
                    var bytes = buildHelper.BuildYMT(true);
                    tasks.Add(buildHelper.BuildFilesAsync(true, bytes, counter));

                    var meta = buildHelper.BuildMeta(true);
                    metaFiles.Add(meta.Name);
                }
                if (selectedAddon.HasFemale)
                {
                    var bytes = buildHelper.BuildYMT(false);
                    tasks.Add(buildHelper.BuildFilesAsync(false, bytes, counter));

                    var meta = buildHelper.BuildMeta(false);
                    metaFiles.Add(meta.Name);
                }
                counter++;
            }
            await Task.WhenAll(tasks);
            buildHelper.BuildFxManifest(metaFiles);

            Close();
            timer.Stop();

            //MessageBox.Show($"Build done, elapsed time: {timer.Elapsed}");
            CustomMessageBox.Show($"Build done, elapsed time: {timer.Elapsed}", "Build done", CustomMessageBoxButtons.OpenFolder, BuildPath);
        }
    }
}
