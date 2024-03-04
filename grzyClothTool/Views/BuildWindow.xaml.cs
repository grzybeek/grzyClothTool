using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private void build_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            if (ProjectName == null || BuildPath == null)
            {
                MessageBox.Show("Please fill in all fields");

                CustomMessageBox.Show($"Please fill in all fields", "Build done", CustomMessageBoxButtons.OKCancel);
                return;
            }
            var timer = new Stopwatch();
            timer.Start();

            var buildHelper = new BuildResourceHelper(MainWindow.Addon, ProjectName, BuildPath);

            var metaFiles = new List<string>();
            if (MainWindow.Addon.HasMale)
            {
                var bytes = buildHelper.BuildYMT();
                buildHelper.BuildFiles(true, bytes);

                var meta = buildHelper.BuildMeta(true);
                metaFiles.Add(meta.Name);
            }
            if (MainWindow.Addon.HasFemale)
            {
                var bytes = buildHelper.BuildYMT();
                buildHelper.BuildFiles(false, bytes);

                var meta = buildHelper.BuildMeta(false);
                metaFiles.Add(meta.Name);
            }

            buildHelper.BuildFxManifest(metaFiles);

            Close();
            timer.Stop();

            //MessageBox.Show($"Build done, elapsed time: {timer.Elapsed}");
            CustomMessageBox.Show($"Build done, elapsed time: {timer.Elapsed}", "Build done", CustomMessageBoxButtons.OpenFolder, BuildPath);
        }
    }
}
