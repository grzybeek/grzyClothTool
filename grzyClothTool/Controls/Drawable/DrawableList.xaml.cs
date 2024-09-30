using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for DrawableList.xaml
    /// </summary>
    public partial class DrawableList : UserControl
    {
        public event EventHandler DrawableListSelectedValueChanged;
        public event KeyEventHandler DrawableListKeyDown;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(ObservableCollection<GDrawable>), typeof(DrawableList), new PropertyMetadata(default(ObservableCollection<GDrawable>)));

        public ObservableCollection<GDrawable> ItemsSource
        {
            get { return (ObservableCollection<GDrawable>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }


        public object DrawableListSelectedValue
        {
            get { return MyListBox.SelectedValue; }
        }

        public DrawableList()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             DrawableListSelectedValueChanged?.Invoke(sender, e);
        }

        private void DrawableList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DrawableListKeyDown?.Invoke(sender, e);
        }

        private void OptimizeTexture_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var drawable = DrawableListSelectedValue as GDrawable;
            FileHelper.OpenFileLocation(drawable.FilePath);
        }

        private void RemoveDrawable_Click(object sender, RoutedEventArgs e)
        {
            var drawable = DrawableListSelectedValue as GDrawable;
            MainWindow.AddonManager.DeleteDrawable(drawable);
        }

        private void ReplaceDrawable_Click(object sender, RoutedEventArgs e)
        {
            var drawable = DrawableListSelectedValue as GDrawable;

            OpenFileDialog files = new()
            {
                Title = $"Select drawable file to replace '{drawable.Name}'",
                Filter = "Drawable files (*.ydd)|*.ydd",
                Multiselect = false
            };

            if (files.ShowDialog() == true)
            {
                drawable.FilePath = files.FileName; // changing just path - might need to be updated to CreateDrawableAsync
                SaveHelper.SetUnsavedChanges(true);

                CWHelper.SendDrawableUpdateToPreview(e);
            }
        }

        async private void ExportDrawable_Click(object sender, RoutedEventArgs e)
        {
            var drawable = DrawableListSelectedValue as GDrawable;

            MenuItem menuItem = sender as MenuItem;
            var tag = menuItem?.Tag?.ToString();


            // pretty ugly way of setting title, might imrpove later
            OpenFolderDialog folder = new()
            {
                Title = tag == null ? "Select the folder to export drawable" :
                        tag == "YTD" ? "Select the folder to export drawable with textures" :
                        $"Select the folder to export textures as {tag}",
                Multiselect = false
            };

            if (folder.ShowDialog() != true)
            {
                return;
            }

            string folderPath = folder.FolderName;

            try
            {
                // Export just textures
                // this is the only case we don't export drawable, so return
                if (tag == "DDS" || tag == "PNG") 
                {
                    await Task.Run(() => FileHelper.SaveTexturesAsync(new List<GTexture>(drawable.Textures), folderPath, tag));
                    return;
                }

                // export drawable
                await Task.Run(() => FileHelper.SaveDrawablesAsync(new List<GDrawable>([drawable]), folderPath));

                if (tag == "YTD") // export drawables as YTD
                {
                    await Task.Run(() => FileHelper.SaveTexturesAsync(new List<GTexture>(drawable.Textures), folderPath, "YTD"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
