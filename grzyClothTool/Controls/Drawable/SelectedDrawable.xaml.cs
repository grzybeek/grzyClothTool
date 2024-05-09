using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Controls
{
    public class DrawableUpdatedArgs : EventArgs
    {
        public string UpdatedName { get; set; }
        public dynamic Value { get; set; }
    }

    public class UpdatedEventArgs : EventArgs
    {
        public DependencyPropertyChangedEventArgs DependencyPropertyChangedEventArgs { get; set; }
        public bool IsUserInitiated { get; set; }
    }

    /// <summary>
    /// Interaction logic for SelectedDrawable.xaml
    /// </summary>
    public partial class SelectedDrawable : UserControl
    {
        private ListBox textureListBox;

        public event EventHandler TextureListSelectedValueChanged;
        public event EventHandler<DrawableUpdatedArgs> SelectedDrawableUpdated;

        public static readonly DependencyProperty SelectedDrawableProperty =
        DependencyProperty.RegisterAttached("SelectedDraw", typeof(GDrawable), typeof(SelectedDrawable), new PropertyMetadata(default(GDrawable)));

        public static readonly DependencyProperty SelectedTextureProperty =
        DependencyProperty.RegisterAttached("SelectedTxt", typeof(GTexture), typeof(SelectedDrawable), new PropertyMetadata(default(GTexture)));

        public static readonly DependencyProperty SelectedTexturesProperty =
         DependencyProperty.RegisterAttached("SelectedTextures", typeof(List<GTexture>), typeof(SelectedDrawable), new PropertyMetadata(default(List<GTexture>)));


        public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.RegisterAttached("SelectedIndex", typeof(int), typeof(SelectedDrawable), new PropertyMetadata(default(int)));

        public GDrawable SelectedDraw
        {
            get { return (GDrawable)GetValue(SelectedDrawableProperty);}
            set { SetValue(SelectedDrawableProperty, value); }
        }

        public GTexture SelectedTxt
        {
            get { return (GTexture)GetValue(SelectedTextureProperty); }
            set { SetValue(SelectedTextureProperty, value); }
        }

        public List<GTexture> SelectedTextures
        {
            get { return (List<GTexture>)GetValue(SelectedTexturesProperty); }
            set { SetValue(SelectedTexturesProperty, value); }
        }

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public SelectedDrawable()
        {
            InitializeComponent(); 
            Loaded += SelectedDrawable_Loaded;
        }

        private void SelectedDrawable_Loaded(object sender, RoutedEventArgs e)
        {
            // Find and store a reference to the TextureListBox
            textureListBox = FindTextureListBox(this);
        }

        private void TextureListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            SelectedTxt = listBox.SelectedItem as GTexture;
            SelectedTextures = listBox.SelectedItems.Cast<GTexture>().ToList();
            TextureListSelectedValueChanged?.Invoke(sender, e);

            if(SelectedTxt == null && listBox.Items.Count >= 1)
            {
                SelectedTxt = listBox.Items[0] as GTexture;
                SelectedTextures = [SelectedTxt];
            }
        }


        private void TexturePreview_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTxt != null)
            {
                Button btn = sender as Button;
                GTexture gtxt = (GTexture)btn.DataContext;

                if (textureListBox != null)
                {
                    textureListBox.SelectedIndex = gtxt.TxtNumber;
                }

                MagickImage img = ImgHelper.GetImage(gtxt.FilePath);
                if (img == null)
                {
                    return;
                }

                int w = img.Width;
                int h = img.Height;
                byte[] pixels = img.ToByteArray(MagickFormat.Bgra);

                Bitmap bitmap = new(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                System.Windows.Controls.Image imageControl = new() { Stretch = Stretch.Uniform, Width = 400, Height = 300 };
                BitmapSource bitmapSource = BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    bitmap.Width * 4
                );

                imageControl.Source = bitmapSource;

                TextBlock textBlock = new()
                {
                    Text = $"{gtxt.DisplayName} ({w}x{h})",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5)
                };

                StackPanel stackPanel = new();
                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(imageControl);

                Border border = new()
                {
                    CornerRadius = new CornerRadius(15),
                    BorderThickness = new Thickness(2),
                    BorderBrush = System.Windows.Media.Brushes.Black,

                    Background = System.Windows.Media.Brushes.White,
                    Child = stackPanel
                };

                Popup popup = new()
                {
                    Width = 400,
                    Height = 350,
                    Placement = PlacementMode.Mouse,
                    StaysOpen = false,
                    Child = border,
                    AllowsTransparency = true,

                    IsOpen = true
                };
                popup.MouseMove += (s, args) =>
                {
                    popup.IsOpen = false;
                };

                popup.Closed += (s, args) =>
                {
                    bitmap.Dispose();
                };
            }
        }

        // Used to notify CW ped viewer of changes to selected drawable
        private void SelectedDrawable_Updated(object sender, UpdatedEventArgs e)
        {
            if (!e.IsUserInitiated)
            {
                return;
            }

            var control = sender as Control;

            var args = new DrawableUpdatedArgs
            {
                UpdatedName = control.Tag.ToString(),
                Value = control.GetValue(e.DependencyPropertyChangedEventArgs.Property)
            };
            SelectedDrawableUpdated?.Invoke(control, args);
            SaveHelper.SetUnsavedChanges(true);
        }

        private static ListBox FindTextureListBox(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListBox listBox && listBox.Name == "TextureListBox")
                {
                    return listBox;
                }

                ListBox result = FindTextureListBox(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void DeleteTexture_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTextures != null)
            {
                foreach (var texture in SelectedTextures)
                {
                    SelectedDraw.Textures.Remove(texture);
                }
                SelectedDraw.Textures.ReassignNumbers();

                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private void AddTexture_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog files = new()
            {
                Title = $"Select textures",
                Filter = "Texture files (*.ytd)|*.ytd|Image files (*.jpg;*.png)|*.jpg;*.png",
                Multiselect = true
            };

            if (files.ShowDialog() == true)
            {
                foreach(var file in files.FileNames)
                {
                    var gtxt = new GTexture(file, SelectedDraw.TypeNumeric, SelectedDraw.Number, SelectedDraw.Textures.Count, SelectedDraw.HasSkin, SelectedDraw.IsProp);
                    SelectedDraw.Textures.Add(gtxt);
                }

                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private void OptimizeTexture_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTextures != null)
            {
                var wrongTextureName = OptimizeWindow.CheckTexturesHaveSameSize(SelectedTextures);
                if (wrongTextureName != null)
                {
                    Show($"Texture {wrongTextureName} does not have the same size as the others!", "Error", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.Error);
                    LogHelper.Log($"Texture {wrongTextureName} does not have the same size as the others!", LogType.Error);
                    return;
                }

                var multipleSelected = SelectedTextures.Count > 1;
                var optimizeWindow = new OptimizeWindow(SelectedTextures, multipleSelected);
                optimizeWindow.ShowDialog();

                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private void DrawableType_Changed(object sender, UpdatedEventArgs e)
        {
            if (!e.IsUserInitiated)
            {
                return;
            }

            var newValue = e.DependencyPropertyChangedEventArgs.NewValue;
            var oldValue = e.DependencyPropertyChangedEventArgs.OldValue;

            if((newValue == null || oldValue == null) || newValue == oldValue)
            {
                return;
            }

            SelectedDraw.ChangeDrawableType(newValue.ToString());
            SaveHelper.SetUnsavedChanges(true);
        }

        private async void ReplaceReserved_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog file = new()
            {
                Title = $"Select drawable file to replace reserved",
                Filter = "Drawable file (*.ydd)|*.ydd"
            };

            if (file.ShowDialog() == true)
            {
                var newDrawable = await FileHelper.CreateDrawableAsync(file.FileName, SelectedDraw.Sex, SelectedDraw.IsProp, SelectedDraw.TypeNumeric, SelectedDraw.Number);

                // Replace reserved drawable with new drawable
                var index = MainWindow.AddonManager.SelectedAddon.Drawables.IndexOf(SelectedDraw);
                MainWindow.AddonManager.SelectedAddon.Drawables[index] = newDrawable;
                SaveHelper.SetUnsavedChanges(true);
            }
        }
    }
}
