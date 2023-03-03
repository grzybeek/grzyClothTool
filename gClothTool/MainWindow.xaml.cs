using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace gClothTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public AddonManager FemaleAddon;
        public AddonManager MaleAddon;

        public MainWindow()
        {
            FemaleAddon = new AddonManager();
            MaleAddon = new AddonManager();

            InitializeComponent();

            FemaleItemsControl.ItemsSource = FemaleAddon.Components;

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = e.Uri.AbsoluteUri;
            p.Start();
        }

        private void Add_Female(object sender, RoutedEventArgs e)
        {
            OpenFileDialog files = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Drawable files (*.ydd)|*.ydd"
            };

            if (files.ShowDialog() == true)
            {
                foreach (string file in files.FileNames)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    Component comp = FemaleAddon.GetComponent(name);
                    comp.AddDrawableToComponent(new Drawable(file, name));

                    //FemaleAddon.Components.Add(new Drawable(file, name));
                }
            }
        }

        private void Add_Male(object sender, RoutedEventArgs e)
        {
            OpenFileDialog files = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Drawable files (*.ydd)|*.ydd"
            };

            if (files.ShowDialog() == true)
            {
                foreach (string file in files.FileNames)
                {
                    string name = Path.GetFileName(file);

                    //MaleAddon.Drawables.Add(new Drawable(file, name));
                }
            }
        }
    }
}
