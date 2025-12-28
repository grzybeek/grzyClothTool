using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using grzyClothTool.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    public partial class DrawableList : UserControl, INotifyPropertyChanged
    {
        public event EventHandler DrawableListSelectedValueChanged;
        public event KeyEventHandler DrawableListKeyDown;
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(ObservableCollection<GDrawable>), typeof(DrawableList), new PropertyMetadata(default(ObservableCollection<GDrawable>), OnItemsSourceChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(DrawableList), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawableList drawableList)
            {
                drawableList.ApplySearchFilter();
            }
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty FilteredCountProperty =
            DependencyProperty.Register("FilteredCount", typeof(int), typeof(DrawableList), new PropertyMetadata(0));

        public int FilteredCount
        {
            get => (int)GetValue(FilteredCountProperty);
            set => SetValue(FilteredCountProperty, value);
        }

        public ObservableCollection<GDrawable> ItemsSource
        {
            get { return (ObservableCollection<GDrawable>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public object DrawableListSelectedValue => MyListBox.SelectedValue;

        private ICollectionView _drawablesView;
        public ICollectionView DrawablesView
        {
            get => _drawablesView;
            set
            {
                _drawablesView = value;
            }
        }

        private bool _isDragging;
        private List<GDrawable> _pendingSelection;
        private static readonly Dictionary<string, bool> value = [];

        private readonly Dictionary<string, bool> _groupExpandedStates = value;
        private bool _isBatchUpdating = false;

        public DrawableList()
        {
            InitializeComponent();
            
            MyListBox.MouseLeave += MyListBox_MouseLeave;
            MyListBox.Loaded += MyListBox_Loaded;
        }

        private void MyListBox_Loaded(object sender, RoutedEventArgs e)
        {
            MyListBox.AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(Expander_StateChanged));
            MyListBox.AddHandler(Expander.CollapsedEvent, new RoutedEventHandler(Expander_StateChanged));
        }

        private void Expander_StateChanged(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Expander expander && expander.DataContext is CollectionViewGroup group)
            {
                var groupName = group.Name as string;
                if (!string.IsNullOrEmpty(groupName))
                {
                    _groupExpandedStates[groupName] = expander.IsExpanded;
                }
            }
        }

        public bool GetGroupExpandedState(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return true;
                
            return !_groupExpandedStates.TryGetValue(groupName, out bool isExpanded) || isExpanded;
        }

        private void MyListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                CleanupGhostLine();
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawableList drawableList)
            {
                drawableList.UnsubscribeFromPropertyChanges(e.OldValue as ObservableCollection<GDrawable>);
                drawableList.SubscribeToPropertyChanges(e.NewValue as ObservableCollection<GDrawable>);
                drawableList.SetupGrouping();
            }
        }

        private void SubscribeToPropertyChanges(ObservableCollection<GDrawable> collection)
        {
            if (collection == null) return;

            foreach (var drawable in collection)
            {
                drawable.PropertyChanged += Drawable_PropertyChanged;
            }

            collection.CollectionChanged += ItemsSource_CollectionChanged;
        }

        private void UnsubscribeFromPropertyChanges(ObservableCollection<GDrawable> collection)
        {
            if (collection == null) return;

            foreach (var drawable in collection)
            {
                drawable.PropertyChanged -= Drawable_PropertyChanged;
            }

            collection.CollectionChanged -= ItemsSource_CollectionChanged;
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (GDrawable drawable in e.NewItems)
                {
                    drawable.PropertyChanged += Drawable_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (GDrawable drawable in e.OldItems)
                {
                    drawable.PropertyChanged -= Drawable_PropertyChanged;
                }
            }
        }

        private void Drawable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GDrawable.Group))
            {
                if (_isBatchUpdating)
                    return;

                _pendingSelection ??= [.. MyListBox.SelectedItems.Cast<GDrawable>()];

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_pendingSelection != null)
                    {
                        DrawablesView?.Refresh();
                        RestoreSelection();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public void BeginBatchUpdate()
        {
            _isBatchUpdating = true;
            _pendingSelection ??= [.. MyListBox.SelectedItems.Cast<GDrawable>()];
        }

        public void EndBatchUpdate()
        {
            _isBatchUpdating = false;
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_pendingSelection != null)
                {
                    DrawablesView?.Refresh();
                    RestoreSelection();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void RestoreSelection()
        {
            if (_pendingSelection != null && _pendingSelection.Any())
            {
                MyListBox.SelectedItems.Clear();

                foreach (var drawable in _pendingSelection)
                {
                    if (ItemsSource?.Contains(drawable) == true)
                    {
                        MyListBox.SelectedItems.Add(drawable);
                    }
                }

                if (MyListBox.SelectedItems.Count > 0)
                {
                    MyListBox.ScrollIntoView(MyListBox.SelectedItems[0]);
                }

                _pendingSelection = null;
            }
        }

        private void SetupGrouping()
        {
            if (ItemsSource == null) return;

            DrawablesView = CollectionViewSource.GetDefaultView(ItemsSource);
            
            if (DrawablesView.GroupDescriptions.Count == 0 || 
                !(DrawablesView.GroupDescriptions[0] is PropertyGroupDescription pgd) ||
                pgd.PropertyName != "Group")
            {
                DrawablesView.GroupDescriptions.Clear();
                DrawablesView.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            }

            DrawablesView.SortDescriptions.Clear();
            if (DrawablesView is ListCollectionView listView)
            {
                listView.CustomSort = new DrawableGroupComparer();
            }

            if (DrawablesView is ICollectionViewLiveShaping liveView && liveView.CanChangeLiveGrouping)
            {
                if (liveView.IsLiveGrouping != true)
                {
                    liveView.LiveGroupingProperties.Clear();
                    liveView.LiveGroupingProperties.Add(nameof(GDrawable.Group));
                    liveView.IsLiveGrouping = true;
                }
                
                if (liveView.CanChangeLiveSorting)
                {
                    liveView.LiveSortingProperties.Clear();
                    liveView.LiveSortingProperties.Add(nameof(GDrawable.Group));
                    liveView.LiveSortingProperties.Add(nameof(GDrawable.Number));
                    liveView.IsLiveSorting = true;
                }
            }
            
            ApplySearchFilter();
            
            // update filtered count when collection changes
            if (ItemsSource is System.Collections.Specialized.INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= OnCollectionChangedForCount;
                notifyCollection.CollectionChanged += OnCollectionChangedForCount;
            }
        }

        private void OnCollectionChangedForCount(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateFilteredCount();
        }

        private void ApplySearchFilter()
        {
            if (DrawablesView == null) return;

            var searchText = SearchText;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                DrawablesView.Filter = null;
            }
            else
            {
                DrawablesView.Filter = FilterDrawable;
            }

            DrawablesView.Refresh();
            UpdateFilteredCount();
        }

        private void UpdateFilteredCount()
        {
            if (DrawablesView == null)
            {
                FilteredCount = ItemsSource?.Count ?? 0;
                return;
            }

            // count visible items
            int count = 0;
            foreach (var item in DrawablesView)
            {
                count++;
            }
            FilteredCount = count;
        }

        private bool FilterDrawable(object item)
        {
            if (item is not GDrawable drawable)
                return false;

            var searchText = SearchText;
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return GetSearchableFields(drawable)
                .Any(field => !string.IsNullOrEmpty(field) && 
                              field.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string?> GetSearchableFields(GDrawable drawable)
        {
            yield return drawable.Name;
            yield return drawable.DisplayName;
            yield return drawable.TypeName;
            yield return drawable.Number.ToString();
            yield return drawable.DisplayNumber;
            yield return drawable.SexName;
            yield return drawable.Group;

            if (drawable.Tags != null)
            {
                foreach (var tag in drawable.Tags)
                {
                    yield return tag;
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawableListSelectedValueChanged?.Invoke(sender, e);

            if (!_isDragging)
            {
                CleanupGhostLine();
            }
        }

        private void DrawableList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            DrawableListKeyDown?.Invoke(sender, e);
        }

        private void CleanupGhostLine()
        {
            if (_ghostLineAdorner != null)
            {
                _adornerLayer?.Remove(_ghostLineAdorner);
                _ghostLineAdorner = null;
            }
            
            if (_currentGroupHeaderBorder != null)
            {
                SetIsDragOver(_currentGroupHeaderBorder, false);
                _currentGroupHeaderBorder = null;
            }
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
            FileHelper.OpenFileLocation(drawable?.FullFilePath);
        }

        private void ShowDuplicateInspector_Click(object sender, RoutedEventArgs e)
        {
            if (DrawableListSelectedValue is GDrawable drawable && drawable.DuplicateInfo.IsDuplicate)
            {
                var inspector = new DuplicateInspectorWindow(drawable);
                inspector.ShowDialog();
            }
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


        private async void ReplaceDrawable_Click(object sender, RoutedEventArgs e)
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
                try
                {
                    // Copy new file to project assets with the drawable's existing GUID
                    var newRelativePath = await FileHelper.CopyToProjectAssetsAsync(files.FileName, drawable.Id.ToString());
                    drawable.FilePath = newRelativePath;
                    SaveHelper.SetUnsavedChanges(true);

                    CWHelper.SendDrawableUpdateToPreview(e);
                    LogHelper.Log($"Replaced drawable '{drawable.Name}' with new file", Views.LogType.Info);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to replace drawable: {ex.Message}", Views.LogType.Error);
                    Show($"Failed to replace drawable: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
                }
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
        private Border _currentGroupHeaderBorder;

        public static readonly DependencyProperty IsDragOverProperty =
            DependencyProperty.RegisterAttached(
                "IsDragOver",
                typeof(bool),
                typeof(DrawableList),
                new PropertyMetadata(false));

        public static bool GetIsDragOver(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragOverProperty);
        }

        public static void SetIsDragOver(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragOverProperty, value);
        }


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

                if (listBoxItem != null && !_isDragging)
                {
                    _isDragging = true;
                    CleanupGhostLine();

                    _adornerLayer = AdornerLayer.GetAdornerLayer(MyListBox);
                    if (_adornerLayer != null)
                    {
                        _ghostLineAdorner = new GhostLineAdorner(MyListBox);
                        _adornerLayer.Add(_ghostLineAdorner);
                        
                        _ghostLineAdorner.UpdatePosition(listBoxItem, false, MyListBox);
                    }

                    var selectedItem = listBox?.SelectedItem;

                    if (selectedItem is GDrawable)
                    {
                        DataObject data = new(typeof(GDrawable), selectedItem);
                        
                        try
                        {
                            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);
                        }
                        finally
                        {
                            _isDragging = false;
                            CleanupGhostLine();
                        }
                    }
                    else
                    {
                        _isDragging = false;
                    }
                }
            }
        }

        private void MyListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (listBoxItem != null && listBoxItem.DataContext is GDrawable drawable)
            {
                if (drawable.IsEncrypted || drawable.IsReserved)
                {
                    return;
                }

                CWHelper.OpenDrawableInPreview(drawable);
                e.Handled = true;
            }
        }

        private void MyListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(GDrawable)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                
                bool isOverGroupHeader = IsOverGroupHeader(e.OriginalSource);
                
                DependencyObject hitElement = e.OriginalSource as DependencyObject;
                
                if (isOverGroupHeader && hitElement != null)
                {
                    Border groupBorder = FindAncestor<Border>(hitElement);
                    if (groupBorder != null && groupBorder.Name == "GroupHeaderBorder")
                    {
                        if (_currentGroupHeaderBorder != null && _currentGroupHeaderBorder != groupBorder)
                        {
                            SetIsDragOver(_currentGroupHeaderBorder, false);
                        }
                        
                        SetIsDragOver(groupBorder, true);
                        _currentGroupHeaderBorder = groupBorder;
                        
                        _ghostLineAdorner?.Hide();
                    }
                }
                else
                {
                    if (_currentGroupHeaderBorder != null)
                    {
                        SetIsDragOver(_currentGroupHeaderBorder, false);
                        _currentGroupHeaderBorder = null;
                    }
                    
                    if (_ghostLineAdorner != null && hitElement != null)
                    {
                        ListBoxItem targetItem = FindAncestor<ListBoxItem>(hitElement);
                        
                        if (targetItem != null)
                        {
                            int index = MyListBox.ItemContainerGenerator.IndexFromContainer(targetItem);
                            if (index >= 0)
                            {
                                _ghostLineAdorner.Show();
                                _ghostLineAdorner.UpdatePositionWithEvent(targetItem, e, MyListBox);
                            }
                        }
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
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private static bool IsOverGroupHeader(object source)
        {
            DependencyObject current = source as DependencyObject;
            
            while (current != null)
            {
                if (current is Border border && border.Name == "GroupHeaderBorder")
                {
                    return true;
                }
                
                if (current is Expander)
                {
                    return true;
                }
                
                if (current is ListBoxItem)
                {
                    return false;
                }
                
                current = VisualTreeHelper.GetParent(current);
            }
            
            return false;
        }

        private void MyListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(GDrawable)))
            {
                bool droppedOnGroupHeader = IsOverGroupHeader(e.OriginalSource);
                string targetGroupName = null;
                
                if (droppedOnGroupHeader)
                {
                    targetGroupName = GetGroupNameFromElement(e.OriginalSource as DependencyObject);
                }
                
                GDrawable target = null;
                if (e.OriginalSource is FrameworkElement element)
                {
                    target = element.DataContext as GDrawable;
                }

                if (e.Data.GetData(typeof(GDrawable)) is GDrawable droppedData && ItemsSource != null)
                {
                    if (droppedOnGroupHeader && targetGroupName != null)
                    {
                        if (droppedData.Group != targetGroupName)
                        {
                            var oldGroup = droppedData.Group;
                            droppedData.Group = targetGroupName;
                            LogHelper.Log($"Drawable '{droppedData.Name}' moved from group '{oldGroup ?? "Ungrouped"}' to '{targetGroupName}'");
                            SaveHelper.SetUnsavedChanges(true);
                        }
                        CleanupGhostLine();
                        return;
                    }
                    
                    if (target == null && !droppedOnGroupHeader)
                    {
                        CleanupGhostLine();
                        return;
                    }

                    if (target != null && (droppedData.Sex != target.Sex || droppedData.TypeNumeric != target.TypeNumeric || droppedData.IsProp != target.IsProp))
                    {
                        CleanupGhostLine();
                        return;
                    }

                    if (target != null && droppedData.Group != target.Group)
                    {
                        var oldGroup = droppedData.Group;
                        droppedData.Group = target.Group;
                        LogHelper.Log($"Drawable '{droppedData.Name}' moved from group '{oldGroup ?? "Ungrouped"}' to '{target.Group ?? "Ungrouped"}'");
                        SaveHelper.SetUnsavedChanges(true);
                    }

                    if (target != null)
                    {
                        int oldIndex = ItemsSource.IndexOf(droppedData);
                        int newIndex = ItemsSource.IndexOf(target);

                        if (oldIndex == newIndex)
                        {
                            CleanupGhostLine();
                            return;
                        }

                        var sameTypeDrawables = ItemsSource
                            .Where(x => x.IsProp == droppedData.IsProp && 
                                       x.Sex == droppedData.Sex && 
                                       x.TypeNumeric == droppedData.TypeNumeric &&
                                       x.Group == droppedData.Group)
                            .OrderBy(x => x.Number)
                            .ToList();

                        int oldVisualIndex = sameTypeDrawables.IndexOf(droppedData);
                        int targetVisualIndex = sameTypeDrawables.IndexOf(target);

                        if (oldVisualIndex == targetVisualIndex)
                        {
                            CleanupGhostLine();
                            return;
                        }

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

                        int newVisualIndex = targetVisualIndex;
                        if (insertAfter)
                        {
                            newVisualIndex = targetVisualIndex + 1;
                        }
                        
                        if (oldVisualIndex < newVisualIndex)
                        {
                            newVisualIndex--;
                        }

                        sameTypeDrawables.RemoveAt(oldVisualIndex);
                        sameTypeDrawables.Insert(newVisualIndex, droppedData);

                        for (int i = 0; i < sameTypeDrawables.Count; i++)
                        {
                            sameTypeDrawables[i].Number = i;
                            sameTypeDrawables[i].SetDrawableName();
                        }

                        LogHelper.Log($"Drawable '{droppedData.Name}' moved from position {oldVisualIndex} to {newVisualIndex}");
                        SaveHelper.SetUnsavedChanges(true);

                        DrawablesView?.Refresh();

                        MyListBox.SelectedItem = droppedData;
                        MyListBox.ScrollIntoView(droppedData);
                    }

                    CleanupGhostLine();
                }
            }
            else
            {
                CleanupGhostLine();
            }
        }

        private static string GetGroupNameFromElement(DependencyObject element)
        {
            DependencyObject current = element;
            
            while (current != null)
            {
                if (current is Expander expander && expander.DataContext is CollectionViewGroup group)
                {
                    return group.Name as string;
                }
                
                if (current is FrameworkElement fe && fe.DataContext is CollectionViewGroup cvg)
                {
                    return cvg.Name as string;
                }
                
                current = VisualTreeHelper.GetParent(current);
            }
            
            return null;
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


    public class DrawableGroupComparer : System.Collections.IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is not GDrawable drawableX || y is not GDrawable drawableY)
                return 0;

            string groupX = drawableX.Group;
            string groupY = drawableY.Group;
            
            bool hasGroupX = !string.IsNullOrEmpty(groupX);
            bool hasGroupY = !string.IsNullOrEmpty(groupY);

            // Both have groups - sort by group name with natural number ordering
            if (hasGroupX && hasGroupY)
            {
                int groupComparison = CompareNatural(groupX, groupY);
                if (groupComparison != 0)
                    return groupComparison;

                // Within the same group, sort by gender (female first), then TypeName alphabetically, then by Number
                int genderComparison = drawableX.Sex.CompareTo(drawableY.Sex);
                if (genderComparison != 0)
                    return genderComparison;

                int typeComparison = string.Compare(drawableX.TypeName, drawableY.TypeName, StringComparison.OrdinalIgnoreCase);
                if (typeComparison != 0)
                    return typeComparison;

                return drawableX.Number.CompareTo(drawableY.Number);
            }

            // X has a group, Y doesn't - X comes first (groups before ungrouped)
            if (hasGroupX && !hasGroupY)
                return -1;

            // Y has a group, X doesn't - Y comes first (groups before ungrouped)
            if (!hasGroupX && hasGroupY)
                return 1;

            // Neither has a group - sort by gender (female first), then TypeName alphabetically, then by Number
            int genderComparisonNoGroup = drawableX.Sex.CompareTo(drawableY.Sex);
            if (genderComparisonNoGroup != 0)
                return genderComparisonNoGroup;

            int typeComparisonNoGroup = string.Compare(drawableX.TypeName, drawableY.TypeName, StringComparison.OrdinalIgnoreCase);
            if (typeComparisonNoGroup != 0)
                return typeComparisonNoGroup;

            return drawableX.Number.CompareTo(drawableY.Number);
        }

        private static int CompareNatural(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int ix = 0, iy = 0;

            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                {
                    int numStartX = ix;
                    while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                    
                    int numStartY = iy;
                    while (iy < y.Length && char.IsDigit(y[iy])) iy++;

                    string numStrX = x.Substring(numStartX, ix - numStartX);
                    string numStrY = y.Substring(numStartY, iy - numStartY);

                    if (int.TryParse(numStrX, out int numX) && int.TryParse(numStrY, out int numY))
                    {
                        int numComparison = numX.CompareTo(numY);
                        if (numComparison != 0)
                            return numComparison;
                    }
                    else
                    {
                        int strComparison = string.Compare(numStrX, numStrY, StringComparison.OrdinalIgnoreCase);
                        if (strComparison != 0)
                            return strComparison;
                    }
                }
                else
                {
                    int charComparison = char.ToLowerInvariant(x[ix]).CompareTo(char.ToLowerInvariant(y[iy]));
                    if (charComparison != 0)
                        return charComparison;
                    ix++;
                    iy++;
                }
            }

            return x.Length.CompareTo(y.Length);
        }
    }

    public class GroupExpandedStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is string groupName &&
                values[1] is DrawableList drawableList)
            {
                return drawableList.GetGroupExpandedState(groupName);
            }

            return true; // Default to expanded
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // We don't need to convert back - the state is captured by the event handler
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

    public class GroupItemToBooleanConverter : IValueConverter
    {
        private static GroupItemToBooleanConverter _instance;
        public static GroupItemToBooleanConverter Instance => _instance ??= new GroupItemToBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is a GroupItem, then this item is inside a group
            return value is GroupItem groupItem && groupItem.DataContext is CollectionViewGroup group && group.Name != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GhostLineAdorner : Adorner
    {
        private readonly Rectangle _ghostLine;
        private bool _isVisible = true;
        private Point _position;

        public GhostLineAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsClipEnabled = false;
            IsHitTestVisible = false;
            
            _ghostLine = new Rectangle
            {
                Height = 3,
                Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Opacity = 1.0,
                StrokeThickness = 0,
                IsHitTestVisible = false
            };
            
            AddVisualChild(_ghostLine);
            AddLogicalChild(_ghostLine);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _ghostLine;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _ghostLine.Measure(constraint);
            return new Size(AdornedElement.RenderSize.Width, 3);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isVisible)
            {
                _ghostLine.Arrange(new Rect(new Point(_position.X, _position.Y), new Size(AdornedElement.RenderSize.Width, 3)));
            }
            else
            {
                _ghostLine.Arrange(new Rect(0, 0, 0, 0));
            }
            return finalSize;
        }

        public void Hide()
        {
            _isVisible = false;
            InvalidateArrange();
            InvalidateVisual();
        }

        public void Show()
        {
            _isVisible = true;
            InvalidateArrange();
            InvalidateVisual();
        }

        public void UpdatePosition(UIElement targetElement, bool isOverGroupHeader, UIElement listBox)
        {
            if (targetElement == null) return;

            var targetPos = targetElement.TransformToAncestor(AdornedElement).Transform(new Point(0, 0));
            var targetHeight = targetElement.RenderSize.Height;
            
            _position = new Point(0, targetPos.Y);
            
            if (listBox != null && Mouse.PrimaryDevice != null)
            {
                var mousePos = Mouse.GetPosition(targetElement);
                
                if (mousePos.Y > targetHeight / 2)
                {
                    _position = new Point(0, targetPos.Y + targetHeight);
                }
            }
            
            InvalidateArrange();
            InvalidateVisual();
        }

        public void UpdatePositionWithEvent(UIElement targetElement, DragEventArgs e, UIElement listBox)
        {
            if (targetElement == null) return;

            var targetPos = targetElement.TransformToAncestor(AdornedElement).Transform(new Point(0, 0));
            var targetHeight = targetElement.RenderSize.Height;
            
            _position = new Point(0, targetPos.Y);
            
            var mousePos = e.GetPosition(targetElement);
            
            if (mousePos.Y > targetHeight / 2)
            {
                _position = new Point(0, targetPos.Y + targetHeight);
            }
            
            InvalidateArrange();
            InvalidateVisual();
        }
    }
}
