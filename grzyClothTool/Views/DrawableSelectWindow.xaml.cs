using grzyClothTool.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace grzyClothTool.Views
{
    public class DrawableSelectItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string FilePath { get; }
        public string FileName => Path.GetFileName(FilePath);

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? IsProp { get; private set; }
        public int? DrawableType { get; private set; }
        public string DrawableTypeName { get; private set; }
        public bool HasAssignedType => DrawableType.HasValue;
        public string AssignedText => HasAssignedType
            ? $"{(IsProp == true ? "Prop" : "Component")} / {DrawableTypeName}"
            : "Not set";

        public DrawableSelectItem(string filePath)
        {
            FilePath = filePath;
        }

        public void SetDrawableType(bool isProp, string drawableTypeName)
        {
            IsProp = isProp;
            DrawableTypeName = drawableTypeName;
            DrawableType = EnumHelper.GetValue(drawableTypeName, isProp);
            OnPropertyChanged(nameof(HasAssignedType));
            OnPropertyChanged(nameof(AssignedText));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Interaction logic for DrawableSelectWindow.xaml
    /// </summary>
    public partial class DrawableSelectWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string AssetPath { get; set; }
        public string HeaderText => DrawableItems.Count == 1
            ? "Could not recognize your drawable properties"
            : $"Could not recognize {DrawableItems.Count} drawable properties";
        public List<string> AssetTypes { get; set; } = ["Component", "Prop"];
        public ObservableCollection<DrawableSelectItem> DrawableItems { get; set; } = [];
        public Dictionary<string, (bool IsProp, int DrawableType)> SelectedDrawableTypes { get; private set; } = [];
        public IEnumerable<DrawableSelectItem> VisibleDrawableItems => ShowTypedItems
            ? DrawableItems
            : DrawableItems.Where(x => !x.HasAssignedType);
        public string SelectionSummary => $"{DrawableItems.Count(x => x.IsSelected)} checked, {DrawableItems.Count(x => x.HasAssignedType)} of {DrawableItems.Count} typed";

        private bool _showTypedItems;
        public bool ShowTypedItems
        {
            get => _showTypedItems;
            set
            {
                if (_showTypedItems != value)
                {
                    _showTypedItems = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisibleDrawableItems));
                }
            }
        }

        private string _selectedAssetType;
        public string SelectedAssetType
        {
            get => _selectedAssetType;
            set
            {
                if (_selectedAssetType != value)
                {
                    _selectedAssetType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsProp));
                    OnPropertyChanged(nameof(IsApplyEnabled));
                }
            }
        }

        public bool IsAssetTypeSelected => DrawableTypes.Count > 0;
        public bool IsApplyEnabled => SelectedDrawableType != null && DrawableItems.Any(x => x.IsSelected);
        public bool IsSubmitEnabled => DrawableItems.Count > 0 &&
            (DrawableItems.All(x => x.HasAssignedType) || (DrawableItems.Count == 1 && SelectedDrawableType != null));
        public bool IsProp => SelectedAssetType == "Prop";

        private List<string> _drawableTypes = [];
        public List<string> DrawableTypes
        {
            get { return _drawableTypes; }
            set
            {
                if (_drawableTypes != value)
                {
                    _drawableTypes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAssetTypeSelected));
                    OnPropertyChanged(nameof(IsApplyEnabled));
                }
            }
        }

        private string _selectedDrawableType;
        public string SelectedDrawableType
        {
            get { return _selectedDrawableType; }
            set
            {
                if (_selectedDrawableType != value)
                {
                    _selectedDrawableType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsApplyEnabled));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        public DrawableSelectWindow(string path)
            : this([path])
        {
            AssetPath = path;
        }

        public DrawableSelectWindow(IEnumerable<string> paths)
        {
            InitializeComponent();
            DataContext = this;

            foreach (var path in paths)
            {
                var item = new DrawableSelectItem(path);
                item.PropertyChanged += DrawableItem_PropertyChanged;
                DrawableItems.Add(item);
            }

            AssetPath = DrawableItems.FirstOrDefault()?.FilePath;
        }

        private void AssetType_IsUpdated(object sender, Controls.UpdatedEventArgs e)
        {
            var newValue = e.DependencyPropertyChangedEventArgs.NewValue;
            if (newValue == null)
            {
                return;
            }

            SelectedAssetType = newValue as string;
            SelectedDrawableType = null;

            DrawableTypes = newValue switch
            {
                "Component" => EnumHelper.GetDrawableTypeList(),
                "Prop" => EnumHelper.GetPropTypeList(),
                _ => [],
            };
        }

        private void ApplySelected_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            ApplySelectedType();
        }

        private void ApplySelectedType()
        {
            if (SelectedDrawableType == null)
            {
                return;
            }

            foreach (var item in DrawableItems.Where(x => x.IsSelected))
            {
                item.SetDrawableType(IsProp, SelectedDrawableType);
                item.IsSelected = false;
            }

            OnDrawableItemsChanged();
        }

        private void Select_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            if (DrawableItems.Count == 1 && !DrawableItems[0].HasAssignedType && SelectedDrawableType != null)
            {
                ApplySelectedType();
            }

            if (!IsSubmitEnabled)
            {
                return;
            }

            SelectedDrawableTypes = DrawableItems.ToDictionary(
                item => item.FilePath,
                item => (item.IsProp == true, item.DrawableType.GetValueOrDefault()));

            DialogResult = true;
            Close();
        }

        private void Cancel_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in VisibleDrawableItems)
            {
                item.IsSelected = true;
            }

            OnDrawableItemsChanged();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in VisibleDrawableItems)
            {
                item.IsSelected = false;
            }

            OnDrawableItemsChanged();
        }

        private void DrawableItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrawableSelectItem.IsSelected) ||
                e.PropertyName == nameof(DrawableSelectItem.HasAssignedType))
            {
                OnDrawableItemsChanged();
            }
        }

        private void DrawableItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DrawableSelectItem item })
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        private void OnDrawableItemsChanged()
        {
            OnPropertyChanged(nameof(VisibleDrawableItems));
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(IsApplyEnabled));
            OnPropertyChanged(nameof(IsSubmitEnabled));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
