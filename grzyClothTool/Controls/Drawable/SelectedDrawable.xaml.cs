using CodeWalker.Utils;
using grzyClothTool.Constants;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using grzyClothTool.Views;
using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
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
        public event EventHandler TextureListSelectedValueChanged;
        public event EventHandler<DrawableUpdatedArgs> SelectedDrawableUpdated;

        public static readonly DependencyProperty SelectedDrawableProperty =
        DependencyProperty.RegisterAttached("SelectedDraw", typeof(GDrawable), typeof(SelectedDrawable), new PropertyMetadata(default(GDrawable)));

        public static readonly DependencyProperty SelectedDrawablesProperty = 
            DependencyProperty.RegisterAttached("SelectedDrawables", typeof(ObservableCollection<GDrawable>), typeof(SelectedDrawable), new PropertyMetadata(default(ObservableCollection<GDrawable>)));

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

        public ObservableCollection<GDrawable> SelectedDrawables
        {
            get { return (ObservableCollection<GDrawable>)GetValue(SelectedDrawablesProperty); }
            set { SetValue(SelectedDrawablesProperty, value); }
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

        // Ghost line adorner for texture drag and drop
        private AdornerLayer _textureAdornerLayer;
        private GhostLineAdorner _textureGhostLineAdorner;

        public SelectedDrawable()
        {
            InitializeComponent();
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

        private void TextureListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (listBoxItem != null && listBoxItem.DataContext is GTexture texture)
            {
                if (texture.IsPreviewDisabled)
                {
                    return;
                }

                if (SelectedDraw != null && !SelectedDraw.IsEncrypted && !SelectedDraw.IsReserved)
                {
                    CWHelper.OpenDrawableInPreview(SelectedDraw);
                }

                e.Handled = true;
            }
        }


        private void TexturePreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                GTexture gtxt = (GTexture)btn.DataContext;

                var textureListBox = FindTextureListBox(this);
                textureListBox.SelectedIndex = gtxt.TxtNumber;

                MagickImage img = ImgHelper.GetImage(gtxt.FullFilePath);
                if (img == null)
                {
                    return;
                }

                int w = (int)img.Width;
                int h = (int)img.Height;
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
            catch (Exception ex)
            {
                LogHelper.Log($"Error displaying texture preview: {ex.Message}", LogType.Error);
            }

        }

        private void EmbeddedTexturePreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;

                if (btn.DataContext is not GTextureEmbedded embeddedTexture)
                    return;

                var textureData = embeddedTexture.DisplayTextureData;
                if (textureData?.Data?.FullData == null || textureData.Data.FullData.Length == 0)
                    return;

                var dds = DDSIO.GetDDSFile(textureData);
                using MagickImage img = new(dds);
                
                int w = (int)img.Width;
                int h = (int)img.Height;
                byte[] pixels = img.ToByteArray(MagickFormat.Bgra);

                Bitmap bitmap = new(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                System.Windows.Controls.Image imageControl = new() { Stretch = Stretch.Uniform, Width = 400, Height = 300 };
                BitmapSource bitmapSource = BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    96, 96,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    bitmap.Width * 4
                );

                imageControl.Source = bitmapSource;

                var statusText = embeddedTexture.HasReplacement ? " - REPLACEMENT" : " - Embedded";
                TextBlock textBlock = new()
                {
                    Text = $"({embeddedTexture.Details.Type}) {embeddedTexture.Details.Name} ({w}x{h}){statusText}",
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
            catch (Exception ex)
            {
                LogHelper.Log($"Error displaying embedded texture preview: {ex.Message}", LogType.Error);
            }
        }

        // Used to notify CW ped viewer of changes to selected drawable
        private async void SelectedDrawable_Updated(object sender, UpdatedEventArgs e)
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

            if (args.Value == null)
            {
                return;
            }

            // Handle FirstPersonPath and ClothPhysicsPath - copy to project assets with proper naming
            if ((args.UpdatedName == "FirstPersonPath" || args.UpdatedName == "ClothPhysicsPath") && 
                control is ModernLabelTextBox textBox)
            {
                var originalPath = textBox.OriginalSelectedPath;
                if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
                {
                    var relativePath = await HandleSpecialFilePath(args.UpdatedName, originalPath);
                    textBox.OriginalSelectedPath = string.Empty;
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        textBox.Text = relativePath;
                    }
                    return;
                }
            }

            SelectedDrawableUpdated?.Invoke(control, args);
            SaveHelper.SetUnsavedChanges(true);


            // when multiple drawables selected, it doesn't update fields automatically, we have to set it from backend
            if (MainWindow.AddonManager.SelectedAddon.IsMultipleDrawablesSelected)
            {
                if (control is ModernLabelComboBox b && b.IsMultiSelect)
                {
                    // it's horrible but multiselect in ModernLabelComboBox needs to be handled differently
                    return;
                }

                var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
                foreach (var drawable in selectedDrawables)
                {
                    var property = drawable.GetType().GetProperty(args.UpdatedName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(drawable, Convert.ChangeType(args.Value, property.PropertyType));
                    }
                }
            }
        }

        /// <summary>
        /// Handles special file paths (FirstPersonPath, ClothPhysicsPath) by copying them to project assets
        /// using the main drawable's GUID with a suffix (e.g., "_firstperson" or "_cloth").
        /// </summary>
        /// <returns>The relative path of the copied file, or null if failed.</returns>
        private async Task<string?> HandleSpecialFilePath(string propertyName, string sourceFilePath)
        {
            try
            {
                if (SelectedDraw == null)
                {
                    LogHelper.Log($"Cannot process {propertyName}: No drawable selected", LogType.Warning);
                    return null;
                }

                string suffix = propertyName switch
                {
                    "FirstPersonPath" => "_firstperson",
                    "ClothPhysicsPath" => "_cloth",
                    _ => throw new ArgumentException($"Unknown property: {propertyName}")
                };

                var fileNameWithoutExtension = $"{SelectedDraw.Id}{suffix}";
                var relativePath = await FileHelper.CopyToProjectAssetsWithReplaceAsync(sourceFilePath, fileNameWithoutExtension);

                if (propertyName == "FirstPersonPath")
                {
                    SelectedDraw.FirstPersonPath = relativePath;
                }
                else if (propertyName == "ClothPhysicsPath")
                {
                    SelectedDraw.ClothPhysicsPath = relativePath;
                }

                SaveHelper.SetUnsavedChanges(true);
                LogHelper.Log($"Copied {propertyName} file to project assets: {relativePath}", LogType.Info);
                return relativePath;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to copy {propertyName} file to project assets: {ex.Message}. Using original path.", LogType.Warning);
                
                if (propertyName == "FirstPersonPath")
                {
                    SelectedDraw.FirstPersonPath = sourceFilePath;
                }
                else if (propertyName == "ClothPhysicsPath")
                {
                    SelectedDraw.ClothPhysicsPath = sourceFilePath;
                }
                
                SaveHelper.SetUnsavedChanges(true);
                return null;
            }
        }

        private void GroupEditor_Changed(object sender, EventArgs e)
        {
            if (sender is GroupEditor groupEditor && MainWindow.AddonManager.SelectedAddon.IsMultipleDrawablesSelected)
            {
                var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
                var newGroup = groupEditor.Group;
                
                var drawableList = FindDrawableList();
                drawableList?.BeginBatchUpdate();
                
                try
                {
                    foreach (var drawable in selectedDrawables)
                    {
                        drawable.Group = newGroup;
                    }
                }
                finally
                {
                    drawableList?.EndBatchUpdate();
                }
                
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private DrawableList FindDrawableList()
        {
            DependencyObject current = this;
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is UserControl userControl && userControl.GetType().Name == "ProjectWindow")
                {
                    return FindVisualChild<DrawableList>(userControl);
                }
            }
            return null;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

    private void TagsEditor_Changed(object sender, EventArgs e)
    {
        if (sender is TagsEditor tagsEditor && MainWindow.AddonManager.SelectedAddon.IsMultipleDrawablesSelected)
        {
            var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
            var editorTags = tagsEditor.Tags?.ToList() ?? [];
            
            var allCurrentTags = selectedDrawables.SelectMany(d => d.Tags).Distinct().ToHashSet();
            var addedTags = editorTags.Where(t => !allCurrentTags.Contains(t)).ToList();
            var removedTags = allCurrentTags.Where(t => !editorTags.Contains(t)).ToList();
            
            foreach (var drawable in selectedDrawables)
            {
                foreach (var tag in addedTags)
                {
                    if (!drawable.Tags.Contains(tag))
                    {
                        drawable.Tags.Add(tag);
                    }
                }
                
                foreach (var tag in removedTags)
                {
                    drawable.Tags.Remove(tag);
                }
            }
            
            SaveHelper.SetUnsavedChanges(true);
        }
    }

        private static ListBox FindTextureListBox(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListBox listBox && (listBox.Name == "TextureListBox" || listBox.Name == "ModernTextureListBox"))
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

        private async void DeleteTexture_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTextures == null || SelectedTextures.Count == 0)
                return;

            var textureListBox = FindTextureListBox(this);
            if (textureListBox == null)
                return;

            int removedIndex = textureListBox.SelectedIndex;
            var sel = SelectedDraw;
            foreach (var texture in SelectedTextures)
            {
                sel.Textures.Remove(texture);

                if (SettingsHelper.Instance.AutoDeleteFiles && File.Exists(texture.FullFilePath))
                {
                    try
                    {
                        File.Delete(texture.FullFilePath);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log($"Failed to delete file {texture.FullFilePath}: {ex.Message}", LogType.Warning);
                    }
                }
            }

            sel.Textures.ReassignNumbers();
            SaveHelper.SetUnsavedChanges(true);

            if (sel.Textures.Count > 0)
            {
                int newIndex = Math.Min(removedIndex, sel.Textures.Count - 1);
                textureListBox.SelectedIndex = newIndex;
                SelectedTxt = textureListBox.SelectedItem as GTexture;
                SelectedTextures = textureListBox.SelectedItems.Cast<GTexture>().ToList();
            }
            else
            {
                SelectedTxt = null;
                SelectedTextures = null;
            }
        }


        private void AddTexture_Click(object sender, RoutedEventArgs e)
        {
            // calculate remaining texures that can be added
            int remainingTextures = GlobalConstants.MAX_DRAWABLE_TEXTURES - SelectedDraw.Textures.Count;
            if (remainingTextures <= 0)
            {
                Show($"You can't have more than {GlobalConstants.MAX_DRAWABLE_TEXTURES} textures per drawable!", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
                return;
            }

            OpenFileDialog files = new()
            {
                Title = $"Select textures",
                Filter = "Texture files (*.ytd)|*.ytd|Image files (*.jpg;*.png;*.dds)|*.jpg;*.png;*.dds",
                Multiselect = true
            };

            if (files.ShowDialog() == true)
            {
                var sel = SelectedDraw;
                foreach (var file in files.FileNames)
                {
                    // check if we are within the limit
                    if (remainingTextures <= 0)
                    {
                        // break the loop and show which texture was the last one
                        Show($"Reached the limit of {GlobalConstants.MAX_DRAWABLE_TEXTURES} textures. Last added texture: {Path.GetFileName(file)}.", "Info", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                        LogHelper.Log($"Reached the limit of {GlobalConstants.MAX_DRAWABLE_TEXTURES} textures. Last added texture: {Path.GetFileName(file)}.", LogType.Warning);
                        break;

                    }
                    var gtxt = new GTexture(Guid.Empty, file, sel.TypeNumeric, sel.Number, sel.Textures.Count, sel.HasSkin, sel.IsProp);
                    gtxt.LoadThumbnailAsync();
                    sel.Textures.Add(gtxt);

                    remainingTextures--;
                }

                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private void HandleTextureOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTextures == null)
                return;

            bool allOptimized = SelectedTextures.All(texture => texture.IsOptimizedDuringBuild);
            bool noneOptimized = SelectedTextures.All(texture => !texture.IsOptimizedDuringBuild);

            if (!allOptimized && !noneOptimized)
            {
                Show("Some textures are already optimized while others are not. Please select textures with the same state.", "Warning", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }

            if (allOptimized)
            {
                UndoTextureOptimization();
            }
            else
            {
                OptimizeTextures();
            }

            SaveHelper.SetUnsavedChanges(true);
        }

        #region Drag and Drop for Textures

        private void TextureListBox_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(GTexture)))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    e.Effects = files.Any(f => IsValidTextureFile(f)) ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ytd", ".jpg", ".png", ".dds");
                    var hasTextureFiles = DragDropHelper.CheckForFilesInDescriptor(e.Data, filter);
                    e.Effects = hasTextureFiles ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error: {ex.Message}", LogType.Error);
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void TextureListBox_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(GTexture)))
                {
                    e.Effects = DragDropEffects.Move;
                    
                    if (_textureGhostLineAdorner != null)
                    {
                        DependencyObject hitElement = e.OriginalSource as DependencyObject;
                        if (hitElement != null)
                        {
                            ListBoxItem targetItem = FindAncestor<ListBoxItem>(hitElement);
                            if (targetItem != null)
                            {
                                _textureGhostLineAdorner.Show();
                                _textureGhostLineAdorner.UpdatePositionWithEvent(targetItem, e, sender as ListBox);
                            }
                        }
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    e.Effects = files.Any(f => IsValidTextureFile(f)) ? DragDropEffects.Copy : DragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error: {ex.Message}", LogType.Error);
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private async void TextureListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (SelectedDraw == null)
                {
                    return;
                }

                if (e.Data.GetDataPresent(typeof(GTexture)))
                {
                    if (e.Data.GetData(typeof(GTexture)) is GTexture droppedTexture && SelectedDraw.Textures.Contains(droppedTexture))
                    {
                        GTexture target = null;
                        if (e.OriginalSource is FrameworkElement element)
                        {
                            target = element.DataContext as GTexture;
                        }

                        if (target != null && SelectedDraw.Textures.Contains(target))
                        {
                            int oldIndex = SelectedDraw.Textures.IndexOf(droppedTexture);
                            int targetIndex = SelectedDraw.Textures.IndexOf(target);

                            if (oldIndex != targetIndex)
                            {
                                bool insertAfter = false;
                                if (e.OriginalSource is DependencyObject depObj)
                                {
                                    var listBoxItem = FindAncestor<ListBoxItem>(depObj);
                                    if (listBoxItem != null)
                                    {
                                        var mousePos = e.GetPosition(listBoxItem);
                                        insertAfter = mousePos.Y > listBoxItem.ActualHeight / 2;
                                    }
                                }

                                int newIndex = targetIndex;
                                if (insertAfter)
                                {
                                    newIndex = targetIndex + 1;
                                }

                                if (oldIndex < newIndex)
                                {
                                    newIndex--;
                                }

                                SelectedDraw.Textures.Move(oldIndex, newIndex);

                                SelectedDraw.Textures.ReassignNumbers();

                                LogHelper.Log($"Texture '{droppedTexture.DisplayName}' moved from position {oldIndex} to {newIndex}");
                                SaveHelper.SetUnsavedChanges(true);

                                var textureListBox = FindTextureListBox(this);
                                if (textureListBox != null)
                                {
                                    textureListBox.SelectedItem = droppedTexture;
                                    textureListBox.ScrollIntoView(droppedTexture);
                                }
                            }
                        }
                    }

                    CleanupTextureGhostLine();
                    e.Handled = true;
                    return;
                }

                List<string> filesToProcess = [];
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    filesToProcess.AddRange(files);
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ytd", ".jpg", ".png", ".dds");
                    var extractedFiles = await DragDropHelper.ExtractVirtualFilesAsync(e.Data, filter);
                    if (extractedFiles.Count > 0)
                    {
                        filesToProcess.AddRange(extractedFiles);
                    }
                    else
                    {
                        LogHelper.Log($"Could not add textures", LogType.Error);
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    e.Handled = true;
                    return;
                }

                var textureFiles = filesToProcess.Where(f => IsValidTextureFile(f)).ToArray();
                if (textureFiles.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                var (accessibleFiles, inaccessibleFiles) = DragDropHelper.ValidateFileAccess(textureFiles);

                if (inaccessibleFiles.Count > 0)
                {
                    var message = $"The following texture file(s) could not be accessed:\n\n" +
                                  string.Join("\n", inaccessibleFiles.Select(Path.GetFileName)) +
                                  "\n\nThey may be virtual paths. Please extract them to a folder first and drag from there.";
                    
                    Show(message, "Files Not Accessible", 
                        CustomMessageBoxButtons.OKOnly, 
                        CustomMessageBoxIcon.Warning);
                }

                if (accessibleFiles.Count == 0)
                {
                    e.Handled = true;
                    return;
                }

                int remainingTextures = GlobalConstants.MAX_DRAWABLE_TEXTURES - SelectedDraw.Textures.Count;
                if (remainingTextures <= 0)
                {
                    Show($"You can't have more than {GlobalConstants.MAX_DRAWABLE_TEXTURES} textures per drawable!", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
                    return;
                }

                var sel = SelectedDraw;
                int addedCount = 0;
                foreach (var file in accessibleFiles)
                {
                    if (remainingTextures <= 0)
                    {
                        Show($"Reached the limit of {GlobalConstants.MAX_DRAWABLE_TEXTURES} textures. Last added texture: {Path.GetFileName(file)}.", "Info", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                        break;
                    }

                    var gtxt = new GTexture(Guid.Empty, file, sel.TypeNumeric, sel.Number, sel.Textures.Count, sel.HasSkin, sel.IsProp);
                    gtxt.LoadThumbnailAsync();
                    sel.Textures.Add(gtxt);

                    remainingTextures--;
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    SaveHelper.SetUnsavedChanges(true);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error: {ex.Message}", LogType.Error);
                
                Show(
                    $"An error occurred while processing dropped texture files:\n\n{ex.Message}",
                    "Drag & Drop Error",
                    CustomMessageBoxButtons.OKOnly,
                    CustomMessageBoxIcon.Error);
            }
            
            e.Handled = true;
        }

        private static bool IsValidTextureFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".ytd" || extension == ".jpg" || extension == ".png" || extension == ".dds";
        }

        #endregion

        private void OptimizeTextures()
        {
            var wrongTextureName = OptimizeWindow.CheckTexturesHaveSameSize(SelectedTextures);
            if (wrongTextureName != null)
            {
                Show($"Texture {wrongTextureName} does not have the same size as the others!", "Error", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.Error);
                LogHelper.Log($"Texture {wrongTextureName} does not have the same size as the others!", LogType.Error);
                return;
            }

            var multipleSelected = SelectedTextures.Count > 1;
            var optimizeWindow = new OptimizeWindow([.. SelectedTextures.Cast<dynamic>()], multipleSelected);
            optimizeWindow.ShowDialog();
        }

        private void UndoTextureOptimization()
        {
            foreach (var texture in SelectedTextures)
            {
                texture.IsOptimizedDuringBuild = false;

                // Deep clone the texture details
                texture.OptimizeDetails = new GTextureDetails
                {
                    Width = texture.TxtDetails.Width,
                    Height = texture.TxtDetails.Height,
                    MipMapCount = texture.TxtDetails.MipMapCount,
                    Compression = texture.TxtDetails.Compression,
                    Name = texture.TxtDetails.Name,
                    IsOptimizeNeeded = texture.TxtDetails.IsOptimizeNeeded,
                    IsOptimizeNeededTooltip = texture.TxtDetails.IsOptimizeNeededTooltip
                };

                LogHelper.Log($"Texture optimization for {texture.DisplayName} has been undone", LogType.Info);
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedTxt != null)
            {
                FileHelper.OpenFileLocation(SelectedTxt.FullFilePath);
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
        }

        private void DrawableSex_Changed(object sender, UpdatedEventArgs e)
        {
            if (!e.IsUserInitiated)
            {
                return;
            }

            var newValue = e.DependencyPropertyChangedEventArgs.NewValue;
            var oldValue = e.DependencyPropertyChangedEventArgs.OldValue;

            if ((newValue == null || oldValue == null) || newValue == oldValue)
            {
                return;
            }


            SelectedDraw.ChangeDrawableSex(newValue.ToString());
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

        private async void ExportTexture_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTextures == null)
            {
                return;
            }

            // Using tags to pass parameters. While functional, a cleaner approach (e.g., CommandParameter) may be preferred
            MenuItem menuItem = sender as MenuItem;
            var format = menuItem?.Tag?.ToString();


            // Make sure we got any format
            if (string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            OpenFolderDialog folder = new()
            {
                Title = $"Select the folder to export textures as {format.ToUpper()}",
                Multiselect = false // Single folder selection
            };

            if (folder.ShowDialog() == true)
            {
                string folderPath = folder.FolderName;

                // Copy the textures to avoid accessing "SelectedTextures" from a background thread
                var texturesToExport = new List<GTexture>(SelectedTextures);

                try
                {
                    await Task.Run(() => FileHelper.SaveTexturesAsync(texturesToExport, folderPath, format));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ReplaceTexture_Click(object sender, RoutedEventArgs e)
        {

            if (SelectedTextures == null || SelectedTextures.Count > 1) // TODO support multiple textures
            {
                return;
            }

            GTexture selectedTexture = SelectedTextures[0];

            OpenFileDialog file = new()
            {
                Title = $"Select texture file to replace {selectedTexture.DisplayName}",
                Filter = "Texture files (*.ytd)|*.ytd|Image files (*.jpg;*.png;*.dds)|*.jpg;*.png;*.dds" // we could store all available formats somewhere
            };

            if (file.ShowDialog() == false)
            {
                return;
            }

            try
            {
                // Copy new texture file to project assets with the texture's existing GUID
                var newRelativePath = await FileHelper.CopyToProjectAssetsAsync(file.FileName, selectedTexture.Id.ToString());
                
                // create new texture with relative path
                var newTexture = new GTexture(selectedTexture.Id, newRelativePath, SelectedDraw.TypeNumeric, SelectedDraw.Number, selectedTexture.TxtNumber, SelectedDraw.HasSkin, SelectedDraw.IsProp);
                int index = SelectedDraw.Textures.IndexOf(selectedTexture);

                MainWindow.AddonManager.SelectedAddon.SelectedDrawable.Textures[index] = newTexture;
                MainWindow.AddonManager.SelectedAddon.SelectedTexture = newTexture;

                SelectedTxt = newTexture;
                SelectedTextures = new List<GTexture>([newTexture]);

                var textureListBox = FindTextureListBox(this);
                textureListBox.SelectedItem = newTexture;

                SaveHelper.SetUnsavedChanges(true);
                CWHelper.SendDrawableUpdateToPreview(e);
                LogHelper.Log($"Replaced texture '{selectedTexture.DisplayName}' with new file", Views.LogType.Info);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to replace texture: {ex.Message}", Views.LogType.Error);
                Show($"Failed to replace texture: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
            }
        }

        private void AllowOverride_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.AddonManager.SelectedAddon.AllowOverrideDrawables = true;
        }

        private void HandleEmbeddedTextureOptimization_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            
            if (menuItem.DataContext is GTextureEmbedded embeddedTexture)
            {
                if (embeddedTexture?.TextureData == null)
                    return;

                if (embeddedTexture.IsOptimizedDuringBuild)
                {
                    UndoEmbeddedTextureOptimization(embeddedTexture);
                }
                else
                {
                    OptimizeEmbeddedTexture(embeddedTexture);
                }

                SaveHelper.SetUnsavedChanges(true);
                return;
            }

            var embeddedTextureEntry = menuItem.DataContext as KeyValuePair<GDrawableDetails.EmbeddedTextureType, GTextureEmbedded>?;
            
            if (!embeddedTextureEntry.HasValue || embeddedTextureEntry.Value.Value?.TextureData == null)
                return;

            var texture = embeddedTextureEntry.Value.Value;

            if (texture.IsOptimizedDuringBuild)
            {
                UndoEmbeddedTextureOptimization(texture);
            }
            else
            {
                OptimizeEmbeddedTexture(texture);
            }

            SaveHelper.SetUnsavedChanges(true);
        }

        private void OptimizeEmbeddedTexture(GTextureEmbedded embeddedTexture)
        {
            var optimizeWindow = new OptimizeWindow([embeddedTexture]);
            optimizeWindow.ShowDialog();
        }

        private void UndoEmbeddedTextureOptimization(GTextureEmbedded embeddedTexture)
        {
            embeddedTexture.IsOptimizedDuringBuild = false;
            
            var currentTexture = embeddedTexture.DisplayTextureData;
            if (currentTexture != null)
            {
                embeddedTexture.OptimizeDetails = new GTextureDetails
                {
                    Width = currentTexture.Width,
                    Height = currentTexture.Height,
                    MipMapCount = currentTexture.Levels,
                    Compression = currentTexture.Format.ToString(),
                    Name = embeddedTexture.Details.Name,
                    IsOptimizeNeeded = embeddedTexture.Details.IsOptimizeNeeded,
                    IsOptimizeNeededTooltip = embeddedTexture.Details.IsOptimizeNeededTooltip
                };
            }

            LogHelper.Log($"Embedded texture optimization for {embeddedTexture.Details.Name} has been undone", LogType.Info);
        }

        private void RenameEmbeddedTexture_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            
            if (menuItem.DataContext is GTextureEmbedded embeddedTexture)
            {
                if (embeddedTexture?.TextureData == null)
                    return;

                var (result, textBoxValue) = Show("Rename Embedded Texture", "Enter new name:", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.None, true);
                if (result == CustomMessageBoxResult.OK)
                {
                    embeddedTexture.RenameTexture(textBoxValue);
                }
                return;
            }

            var embeddedTextureEntry = menuItem.DataContext as KeyValuePair<GDrawableDetails.EmbeddedTextureType, GTextureEmbedded>?;
            
            if (!embeddedTextureEntry.HasValue || embeddedTextureEntry.Value.Value?.TextureData == null)
                return;

            var texture = embeddedTextureEntry.Value.Value;
            var (res, txtValue) = Show("Rename Embedded Texture", "Enter new name:", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.None, true);
            if (res == CustomMessageBoxResult.OK)
            {
                texture.RenameTexture(txtValue);
            }
        }

        private void ReplaceEmbeddedTexture_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            
            GTextureEmbedded embeddedTexture = null;
            GDrawableDetails.EmbeddedTextureType textureType = GDrawableDetails.EmbeddedTextureType.Normal;

            if (menuItem.DataContext is GTextureEmbedded texture)
            {
                if (texture?.TextureData == null)
                    return;
                    
                embeddedTexture = texture;
                if (Enum.TryParse<GDrawableDetails.EmbeddedTextureType>(texture.Details.Type, out var parsedType))
                {
                    textureType = parsedType;
                }
            }
            else
            {
                var embeddedTextureEntry = menuItem.DataContext as KeyValuePair<GDrawableDetails.EmbeddedTextureType, GTextureEmbedded>?;
                
                if (!embeddedTextureEntry.HasValue)
                    return;

                textureType = embeddedTextureEntry.Value.Key;
                embeddedTexture = embeddedTextureEntry.Value.Value;
            }

            if (embeddedTexture == null)
                return;
            
            OpenFileDialog file = new()
            {
                Title = $"Select texture file to replace embedded {textureType}",
                Filter = "Image files (*.jpg;*.png;*.dds)|*.jpg;*.png;*.dds"
            };

            if (file.ShowDialog() == true)
            {
                try
                {
                    var newTextureData = LoadTextureAsEmbedded(file.FileName, textureType.ToString());
                    
                    embeddedTexture.SetReplacementTexture(newTextureData);

                    SaveHelper.SetUnsavedChanges(true);
                    LogHelper.Log($"Embedded {textureType} texture replaced (optimization cleared)", LogType.Info);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to replace embedded texture: {ex.Message}", LogType.Error);
                    CustomMessageBox.Show($"Failed to replace embedded texture: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
                }
            }
        }

        private CodeWalker.GameFiles.Texture LoadTextureAsEmbedded(string filePath, string textureType)
        {
            var img = ImgHelper.GetImage(filePath);
            if (img == null)
            {
                throw new Exception("Failed to load image from the specified file.");
            }

            img.Format = MagickFormat.Dds;

            var txt = DDSIO.GetTexture(img.ToByteArray());
            txt.Name = Path.GetFileNameWithoutExtension(filePath);
            return txt;
        }

        #region Texture Drag and Drop Reordering

        private System.Windows.Point _textureDragStartPoint;
        private bool _isTextureDragging;

        private void TextureListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _textureDragStartPoint = e.GetPosition(null);
        }

        private void TextureListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            Vector diff = _textureDragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is not ListBox listBox) return;

                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
                if (listBoxItem != null && !_isTextureDragging)
                {
                    _isTextureDragging = true;
                    CleanupTextureGhostLine();

                    _textureAdornerLayer = AdornerLayer.GetAdornerLayer(listBox);
                    if (_textureAdornerLayer != null)
                    {
                        _textureGhostLineAdorner = new GhostLineAdorner(listBox);
                        _textureAdornerLayer.Add(_textureGhostLineAdorner);
                        
                        _textureGhostLineAdorner.UpdatePosition(listBoxItem, false, listBox);
                    }

                    var selectedItem = listBox.SelectedItem;
                    if (selectedItem is GTexture)
                    {
                        DataObject data = new(typeof(GTexture), selectedItem);
                        
                        try
                        {
                            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);
                        }
                        finally
                        {
                            _isTextureDragging = false;
                            CleanupTextureGhostLine();
                        }
                    }
                    else
                    {
                        _isTextureDragging = false;
                    }
                }
            }
        }

        private void CleanupTextureGhostLine()
        {
            if (_textureGhostLineAdorner != null)
            {
                _textureAdornerLayer?.Remove(_textureGhostLineAdorner);
                _textureGhostLineAdorner = null;
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t)
                {
                    return t;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }


        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        private void TexturesTab_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchToTab(isTextures: true, sender);
        }

        private void EmbeddedTexturesTab_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchToTab(isTextures: false, sender);
        }

        private void SwitchToTab(bool isTextures, object sender)
        {
            if (sender is not Border clickedTab) return;

            DependencyObject parent = VisualTreeHelper.GetParent(clickedTab);
            while (parent != null && !(parent is Grid))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent == null) return;


            if (FindChildByName(parent, "TexturesTab") is not Border texturesTab || FindChildByName(parent, "EmbeddedTexturesTab") is not Border embeddedTexturesTab ||
                FindChildByName(parent, "TexturesContent") is not ScrollViewer texturesContent || FindChildByName(parent, "EmbeddedTexturesContent") is not ScrollViewer embeddedTexturesContent)
                return;

            Border actionBarBorder = FindChildByName(parent, "ActionBarBorder") as Border;

            if (isTextures)
            {
                texturesTab.BorderBrush = (System.Windows.Media.Brush)FindResource("Brush500");
                embeddedTexturesTab.BorderBrush = System.Windows.Media.Brushes.Transparent;

                texturesContent.Visibility = Visibility.Visible;
                embeddedTexturesContent.Visibility = Visibility.Collapsed;
                
                if (actionBarBorder != null) actionBarBorder.Visibility = Visibility.Visible;
            }
            else
            {
                embeddedTexturesTab.BorderBrush = (System.Windows.Media.Brush)FindResource("Brush500");
                texturesTab.BorderBrush = System.Windows.Media.Brushes.Transparent;

                texturesContent.Visibility = Visibility.Collapsed;
                embeddedTexturesContent.Visibility = Visibility.Visible;
                
                if (actionBarBorder != null) actionBarBorder.Visibility = Visibility.Collapsed;
            }
        }

        private DependencyObject FindChildByName(DependencyObject parent, string name)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement element && element.Name == name)
                {
                    return child;
                }

                var result = FindChildByName(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        #endregion
    }
}
