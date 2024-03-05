using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace grzyClothTool.Controls
{
    public class DrawableUpdatedArgs : EventArgs
    {
        public string UpdatedName { get; set; }
        public dynamic Value { get; set; }
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
        DependencyProperty.RegisterAttached("SelectedDraw", typeof(Models.GDrawable), typeof(SelectedDrawable), new PropertyMetadata(default(Models.GDrawable)));

        public static readonly DependencyProperty SelectedTextureProperty =
        DependencyProperty.RegisterAttached("SelectedTxt", typeof(Models.GTexture), typeof(SelectedDrawable), new PropertyMetadata(default(Models.GTexture)));

        public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.RegisterAttached("SelectedIndex", typeof(int), typeof(SelectedDrawable), new PropertyMetadata(default(int)));

        public Models.GDrawable SelectedDraw
        {
            get { return (Models.GDrawable)GetValue(SelectedDrawableProperty);}
            set { SetValue(SelectedDrawableProperty, value); }
        }

        public Models.GTexture SelectedTxt
        {
            get { return (Models.GTexture)GetValue(SelectedTextureProperty); }
            set { SetValue(SelectedTextureProperty, value); }
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
            TextureListSelectedValueChanged?.Invoke(sender, e);
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

                var ytd = CWHelper.GetYtdFile(gtxt.File.FullName);
                var txt = ytd.TextureDict.Textures[0];
                var pixels = CodeWalker.Utils.DDSIO.GetPixels(txt, 0);

                var w = txt.Width;
                var h = txt.Height;
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

        private void TextureRemove_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedTxt != null)
            {
                //todo: handle removing of texture
            }
        }

        private void TextureRename_Click(object sender, RoutedEventArgs e)
        {
            //todo: handle renaming of texture
        }


        // Used to notify CW ped viewer of changes to selected drawable
        private void SelectedDrawable_Updated(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as Control;

            var args = new DrawableUpdatedArgs
            {
                UpdatedName = control.Tag.ToString(),
                Value = control.GetValue(e.Property)
            };
            SelectedDrawableUpdated?.Invoke(control, args);
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
    }
}
