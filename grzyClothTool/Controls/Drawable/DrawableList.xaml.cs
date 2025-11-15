using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static grzyClothTool.Controls.CustomMessageBox;

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

        public object DrawableListSelectedValue => MyListBox.SelectedValue;

        public DrawableList()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawableListSelectedValueChanged?.Invoke(sender, e);

            if (_ghostLineAdorner != null)
            {
                _adornerLayer?.Remove(_ghostLineAdorner);
                _ghostLineAdorner = null;
            }
        }

        private void DrawableList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            DrawableListKeyDown?.Invoke(sender, e);
        }

        private void MoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem?.Header is string addonName)
            {
                var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
                var addon = MainWindow.AddonManager.Addons.FirstOrDefault(a => a.Name == addonName);

                if (addon == null)
                {
                    return;
                }

                if (!addon.CanFitDrawables(selectedDrawables))
                {
                    Show("The selected addon cannot fit the selected drawables.", "Addon full", CustomMessageBoxButtons.OKOnly);
                    return;
                }

                foreach (var drawable in selectedDrawables)
                {
                    MainWindow.AddonManager.MoveDrawable(drawable, addon);
                }

                MainWindow.AddonManager.Addons.Sort(true);
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var drawable = DrawableListSelectedValue as GDrawable;
            FileHelper.OpenFileLocation(drawable?.FilePath);
        }

        private void DeleteDrawable_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
            MainWindow.AddonManager.DeleteDrawables(selectedDrawables);
        }

        private void DuplicateToOppositeGender_Click(object sender, RoutedEventArgs e)
        {
            if (DrawableListSelectedValue is not GDrawable drawable)
                return;

            var oppositeGender = drawable.Sex == Enums.SexType.male ? Enums.SexType.female : Enums.SexType.male;

            try
            {
                var newTextures = new ObservableCollection<GTexture>();
                foreach (var texture in drawable.Textures)
                {
                    var newTexture = new GTexture(
                        Guid.Empty,
                        texture.FilePath,
                        texture.TypeNumeric,
                        texture.Number,
                        texture.TxtNumber,
                        texture.HasSkin,
                        texture.IsProp
                    )
                    {
                        IsOptimizedDuringBuild = texture.IsOptimizedDuringBuild
                    };

                    if (texture.IsOptimizedDuringBuild && texture.OptimizeDetails != null)
                    {
                        newTexture.OptimizeDetails = new GTextureDetails
                        {
                            Width = texture.OptimizeDetails.Width,
                            Height = texture.OptimizeDetails.Height,
                            MipMapCount = texture.OptimizeDetails.MipMapCount,
                            Compression = texture.OptimizeDetails.Compression,
                            Name = texture.OptimizeDetails.Name,
                            IsOptimizeNeeded = texture.OptimizeDetails.IsOptimizeNeeded,
                            IsOptimizeNeededTooltip = texture.OptimizeDetails.IsOptimizeNeededTooltip
                        };
                    }
                    newTexture.LoadThumbnailAsync();
                    newTextures.Add(newTexture);
                }

                var newDrawable = new GDrawable(
                    Guid.NewGuid(),
                    drawable.FilePath,
                    oppositeGender,
                    drawable.IsProp,
                    drawable.TypeNumeric,
                    0, // Number will be assigned by AddDrawable
                    drawable.HasSkin,
                    newTextures
                )
                {
                    Audio = drawable.Audio,
                    EnableHighHeels = drawable.EnableHighHeels,
                    HighHeelsValue = drawable.HighHeelsValue,
                    EnableHairScale = drawable.EnableHairScale,
                    HairScaleValue = drawable.HairScaleValue,
                    EnableKeepPreview = drawable.EnableKeepPreview,
                    RenderFlag = drawable.RenderFlag,
                    FirstPersonPath = drawable.FirstPersonPath,
                    ClothPhysicsPath = drawable.ClothPhysicsPath
                };

                newDrawable.SelectedFlags.Clear();
                foreach (var flag in drawable.SelectedFlags)
                {
                    newDrawable.SelectedFlags.Add(new SelectableItem(flag.Text, flag.Value, flag.IsSelected));
                }

                MainWindow.AddonManager.AddDrawable(newDrawable);
                MainWindow.AddonManager.Addons.Sort(true);
                
                SaveHelper.SetUnsavedChanges(true);
                LogHelper.Log($"Duplicated drawable '{drawable.Name}' to opposite gender ({oppositeGender}) - new drawable is {newDrawable.Name}");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error duplicating drawable to opposite gender: {ex.Message}", Views.LogType.Error);
                Show($"Failed to duplicate drawable: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
            }
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

        private async void ExportDrawable_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();

            MenuItem menuItem = sender as MenuItem;
            var tag = menuItem?.Tag?.ToString();

            OpenFolderDialog folder = new()
            {
                Title = tag switch
                {
                    "DDS" or "PNG" => $"Select the folder to export textures as {tag}",
                    "YTD" => "Select the folder to export drawable with textures",
                    _ => "Select the folder to export drawable"
                },
                Multiselect = false
            };

            if (folder.ShowDialog() != true)
            {
                return;
            }

            string folderPath = folder.FolderName;

            try
            {
                if (!string.IsNullOrEmpty(tag) && (tag == "YTD" || tag == "PNG" || tag == "DDS"))
                {
                    foreach (var drawable in selectedDrawables)
                    {
                        await Task.Run(() => FileHelper.SaveTexturesAsync(new List<GTexture>(drawable.Textures), folderPath, tag).ConfigureAwait(false));
                    }

                    if (tag == "DDS" || tag == "PNG")
                    {
                        return;
                    }
                }

                await FileHelper.SaveDrawablesAsync(selectedDrawables, folderPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Drag and Drop

        private Point _dragStartPoint;
        private AdornerLayer _adornerLayer;
        private GhostLineAdorner _ghostLineAdorner;


        private void MyListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void MyListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListBox listBox = sender as ListBox;
                ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem != null)
                {
                    if (_ghostLineAdorner != null)
                    {
                        _adornerLayer?.Remove(_ghostLineAdorner);
                        _ghostLineAdorner = null;
                    }

                    _ghostLineAdorner = new GhostLineAdorner(MyListBox);

                    _adornerLayer = AdornerLayer.GetAdornerLayer(MyListBox);
                    _adornerLayer?.Add(_ghostLineAdorner);

                    var selectedItem = listBox?.SelectedItem;

                    if (selectedItem is GDrawable)
                    {
                        DataObject data = new DataObject(typeof(GDrawable), selectedItem);
                        DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void MyListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            Point position = e.GetPosition(MyListBox);
            int index = GetCurrentIndex(position);

            if (_ghostLineAdorner != null)
            {
                if (index >= 0 && index < ItemsSource.Count)
                {
                    _ghostLineAdorner.UpdatePosition(index);
                }
            }

            var scrollViewer = FindChildOfType<ScrollViewer>(MyListBox);
            if (scrollViewer != null)
            {
                const double heightOfAutoScrollZone = 35;
                double mousePos = e.GetPosition(MyListBox).Y;

                if (mousePos < heightOfAutoScrollZone)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 1);
                }
                else if (mousePos > MyListBox.ActualHeight - heightOfAutoScrollZone)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 1);
                }
            }
        }

        private void MyListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(GDrawable)))
            {
                GDrawable droppedData = e.Data.GetData(typeof(GDrawable)) as GDrawable;
                ListBox listBox = sender as ListBox;
                GDrawable target = ((FrameworkElement)e.OriginalSource).DataContext as GDrawable;

                if (droppedData != null && target != null && ItemsSource != null)
                {
                    if (droppedData.Sex != target.Sex || droppedData.TypeNumeric != target.TypeNumeric || droppedData.IsProp != target.IsProp)
                    {
                        return;
                    }

                    int oldIndex = ItemsSource.IndexOf(droppedData);
                    int newIndex = ItemsSource.IndexOf(target);

                    if (oldIndex == newIndex)
                        return; // No movement needed

                    if (oldIndex < newIndex)
                    {
                        for (int i = oldIndex; i < newIndex; i++)
                        {
                            (ItemsSource[i + 1], ItemsSource[i]) = (ItemsSource[i], ItemsSource[i + 1]);
                        }
                    }
                    else
                    {
                        for (int i = oldIndex; i > newIndex; i--)
                        {
                            (ItemsSource[i - 1], ItemsSource[i]) = (ItemsSource[i], ItemsSource[i - 1]);
                        }
                    }

                    LogHelper.Log($"Drawable '{droppedData.Name}' moved from position {oldIndex} to {newIndex}");
                    MainWindow.AddonManager.SelectedAddon.Drawables.ReassignNumbers(droppedData);

                    MyListBox.SelectedItem = droppedData;
                    MyListBox.ScrollIntoView(droppedData);


                    _adornerLayer?.Remove(_ghostLineAdorner);
                    _ghostLineAdorner = null;
                }
            }
        }

        private int GetCurrentIndex(Point position)
        {
            int index = -1;
            for (int i = 0; i < MyListBox.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)MyListBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (item != null)
                {
                    Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
                    Point topLeft = item.TranslatePoint(new Point(), MyListBox);
                    Rect itemBounds = new(topLeft, bounds.Size);

                    if (itemBounds.Contains(position))
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
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
            };
            return null;
        }

        private static TChild FindChildOfType<TChild>(DependencyObject parent) where TChild : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                var result = (child as TChild) ?? FindChildOfType<TChild>(child);
                if (result != null) return result;
            }
            return null;
        }

        #endregion
    }

    public class GhostLineAdorner : Adorner
    {
        private readonly Rectangle _ghostLine;
        private int _index;

        public GhostLineAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _ghostLine = new Rectangle
            {
                Height = 4,
                Width = adornedElement.RenderSize.Width,
                Fill = Brushes.Black,
                Opacity = 1,
                StrokeThickness = 2,
                Stroke = Brushes.Black,
                IsHitTestVisible = false
            };
            AddVisualChild(_ghostLine);
        }

        public void UpdatePosition(int index)
        {
            _index = index;
            InvalidateArrange();
            InvalidateVisual();
            AdornedElement.InvalidateVisual();
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _ghostLine;

        protected override Size MeasureOverride(Size constraint)
        {
            _ghostLine.Measure(constraint);
            return new Size(constraint.Width, _ghostLine.DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_index < 0) return finalSize;

            ListBox listBox = AdornedElement as ListBox;
            if (listBox == null) return finalSize;

            ListBoxItem item = listBox.ItemContainerGenerator.ContainerFromIndex(_index) as ListBoxItem;
            if (item != null)
            {
                Point relativePosition = item.TransformToAncestor(listBox).Transform(new Point(0, 0));
                double itemHeight = item.ActualHeight;

                double yOffset = relativePosition.Y + itemHeight;

                _ghostLine.Arrange(new Rect(new Point(0, yOffset), new Size(finalSize.Width, _ghostLine.Height)));
            }

            return finalSize;
        }
    }
}
