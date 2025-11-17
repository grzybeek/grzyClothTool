using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    public class SelectableItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Text { get; set; }
        public int Value { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public SelectableItem(string text, int value, bool isSelected = false)
        {
            Text = text;
            Value = value;
            IsSelected = isSelected;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => Text ?? string.Empty;
    }

    /// <summary>
    /// Interaction logic for ModernLabelComboBox.xaml
    /// </summary>
    public partial class ModernLabelComboBox : ModernLabelBaseControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
            .Register("Label",
                    typeof(string),
                    typeof(ModernLabelComboBox),
                    new FrameworkPropertyMetadata("Placeholder"));

        public static readonly DependencyProperty TextProperty = DependencyProperty
        .Register("Text",
                typeof(string),
                typeof(ModernLabelComboBox),
                new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
            .Register("SelectedItem",
                    typeof(object),
                    typeof(ModernLabelComboBox),
                    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty
            .Register("SelectedItems",
                    typeof(ObservableCollection<SelectableItem>),
                    typeof(ModernLabelComboBox),
                    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
            .Register("ItemsSource",
                    typeof(IEnumerable),
                    typeof(ModernLabelComboBox),
                    new PropertyMetadata(null));

        public static readonly DependencyProperty ItemsSourceSelectableProperty = DependencyProperty
            .Register("ItemsSourceSelectable",
                    typeof(IEnumerable<SelectableItem>),
                    typeof(ModernLabelComboBox),
                    new PropertyMetadata(null));

        public static readonly DependencyProperty IsMultiSelectProperty = DependencyProperty
            .Register("IsMultiSelect",
                    typeof(bool),
                    typeof(ModernLabelComboBox),
                    new PropertyMetadata(false));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public ObservableCollection<SelectableItem> SelectedItems
        {
            get
            {
                return (ObservableCollection<SelectableItem>)GetValue(SelectedItemsProperty);
            }
            set
            {
                SetValue(SelectedItemsProperty, value);
            }
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public IEnumerable<SelectableItem> ItemsSourceSelectable
        {
            get { return (IEnumerable<SelectableItem>)GetValue(ItemsSourceSelectableProperty); }
            set { SetValue(ItemsSourceSelectableProperty, value); }
        }

        public bool IsMultiSelect
        {
            get { return (bool)GetValue(IsMultiSelectProperty); }
            set { SetValue(IsMultiSelectProperty, value); }
        }

        static ModernLabelComboBox()
        {
            FontSizeProperty.OverrideMetadata(
                typeof(ModernLabelComboBox),
                new FrameworkPropertyMetadata(14.0));
        }

        public ModernLabelComboBox()
        {
            InitializeComponent();
            MyComboBox.DropDownOpened += (s, e) => IsUserInitiated = true;
            MyComboBox.DropDownClosed += (s, e) => IsUserInitiated = false;
            MyComboBox.SelectionChanged += MyComboBox_SelectionChanged;
            
            Loaded += (s, e) => UpdateClearButtonVisibility();
        }

        private void UpdateClearButtonVisibility()
        {
            if (Tag?.ToString() == "Group" && !IsMultiSelect)
            {
                ClearButton.Visibility = SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                ClearButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            MyComboBox.SelectedItem = null;
            
            if (MainWindow.AddonManager?.SelectedAddon != null)
            {
                if (MainWindow.AddonManager.SelectedAddon.IsMultipleDrawablesSelected)
                {
                    var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
                    foreach (var drawable in selectedDrawables)
                    {
                        drawable.Group = null;
                    }
                    Helpers.LogHelper.Log($"Cleared group from {selectedDrawables.Count} drawable(s)", Views.LogType.Info);
                }
                else if (MainWindow.AddonManager.SelectedAddon.SelectedDrawable != null)
                {
                    MainWindow.AddonManager.SelectedAddon.SelectedDrawable.Group = null;
                    Helpers.LogHelper.Log($"Cleared group from drawable '{MainWindow.AddonManager.SelectedAddon.SelectedDrawable.Name}'", Views.LogType.Info);
                }
                
                Helpers.SaveHelper.SetUnsavedChanges(true);
            }
            
            UpdateClearButtonVisibility();
        }

        private void MyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateClearButtonVisibility();

            if (IsMultiSelect)
            {
                // Directly access the "NONE" item assuming it always exists
                SelectableItem noneItem = ItemsSourceSelectable.FirstOrDefault(item => item.Value == (int)Enums.DrawableFlags.NONE);

                foreach (var addedItem in e.AddedItems)
                {
                    if (addedItem is SelectableItem selectableItem)
                    {
                        selectableItem.IsSelected = !selectableItem.IsSelected;

                        if (selectableItem.IsSelected)
                        {
                            if (selectableItem.Value != (int)Enums.DrawableFlags.NONE && noneItem != null && noneItem.IsSelected)
                            {
                                // Deselect "NONE" if another flag is selected
                                noneItem.IsSelected = false;
                                SelectedItems.Remove(noneItem);
                            }

                            if (!SelectedItems.Contains(selectableItem))
                            {
                                SelectedItems.Add(selectableItem);
                            }
                        }
                        else
                        {
                            var item = SelectedItems.Where(x => x.Value == selectableItem.Value).FirstOrDefault();
                            SelectedItems.Remove(item);

                            // If no flags are selected, automatically select "NONE"
                            if (SelectedItems.Count == 0 && noneItem != null)
                            {
                                noneItem.IsSelected = true;
                                SelectedItems.Add(noneItem);
                            }
                        }
                    }
                }

                MyComboBox.SelectedItem = null;
                e.Handled = true;
                SetValue(SelectedItemsProperty, SelectedItems);


                // when multiple drawables selected, it doesn't update fields automatically, we have to set it from backend
                if (MainWindow.AddonManager.SelectedAddon.IsMultipleDrawablesSelected)
                {
                    var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables.ToList();
                    foreach (var drawable in selectedDrawables)
                    {
                        drawable.SelectedFlags = new ObservableCollection<SelectableItem>(SelectedItems);
                    }
                }
            }
        }

    }
}
