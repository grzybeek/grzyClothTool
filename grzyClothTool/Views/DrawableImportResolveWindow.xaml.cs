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
    public class DrawableImportResolveItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string FilePath { get; }
        public string FileName => Path.GetFileName(FilePath);
        public bool RequiresGender { get; }
        public bool RequiresDrawableType { get; }

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

        public Enums.SexType? Gender { get; private set; }
        public bool? IsProp { get; private set; }
        public int? DrawableType { get; private set; }
        public string DrawableTypeName { get; private set; }

        public bool HasGender => Gender.HasValue;
        public bool HasDrawableType => DrawableType.HasValue;
        public bool IsResolved =>
            (!RequiresGender || HasGender) &&
            (!RequiresDrawableType || HasDrawableType);

        public string GenderText => HasGender ? GetGenderDisplayName(Gender.Value) : "Gender not set";
        public string DrawableText => HasDrawableType
            ? $"{(IsProp == true ? "Prop" : "Component")} / {DrawableTypeName}"
            : "Properties not set";

        public DrawableImportResolveItem(
            string filePath,
            Enums.SexType? gender,
            (bool IsProp, int DrawableType)? drawableType,
            bool requiresGender,
            bool requiresDrawableType)
        {
            FilePath = filePath;
            Gender = gender;
            RequiresGender = requiresGender;
            RequiresDrawableType = requiresDrawableType;

            if (drawableType.HasValue)
            {
                SetDrawableType(drawableType.Value.IsProp, drawableType.Value.DrawableType);
            }
        }

        public void SetGender(Enums.SexType gender)
        {
            Gender = gender;
            OnResolvedPropertiesChanged();
        }

        public void SetDrawableType(bool isProp, string drawableTypeName)
        {
            IsProp = isProp;
            DrawableTypeName = drawableTypeName;
            DrawableType = EnumHelper.GetValue(drawableTypeName, isProp);
            OnResolvedPropertiesChanged();
        }

        private void SetDrawableType(bool isProp, int drawableType)
        {
            IsProp = isProp;
            DrawableType = drawableType;
            DrawableTypeName = EnumHelper.GetName(drawableType, isProp);
            OnResolvedPropertiesChanged();
        }

        public static string GetGenderDisplayName(Enums.SexType gender)
        {
            return gender == Enums.SexType.male ? "Male" : "Female";
        }

        private void OnResolvedPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasGender));
            OnPropertyChanged(nameof(HasDrawableType));
            OnPropertyChanged(nameof(IsResolved));
            OnPropertyChanged(nameof(GenderText));
            OnPropertyChanged(nameof(DrawableText));
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class DrawableImportResolveWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly bool _showGender;
        private readonly bool _showDrawableProperties;
        private bool _isApplyingBulkUpdate;

        public List<string> GenderTypes { get; set; } = ["Female", "Male"];
        public List<string> AssetTypes { get; set; } = ["Component", "Prop"];
        public ObservableCollection<DrawableImportResolveItem> Items { get; set; } = [];
        public Dictionary<string, Enums.SexType> SelectedGenders { get; private set; } = [];
        public Dictionary<string, (bool IsProp, int DrawableType)> SelectedDrawableTypes { get; private set; } = [];

        public Visibility GenderTextVisibility => _showGender ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DrawableTextVisibility => _showDrawableProperties ? Visibility.Visible : Visibility.Collapsed;
        public Visibility GenderControlsVisibility => _showGender ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DrawableControlsVisibility => _showDrawableProperties ? Visibility.Visible : Visibility.Collapsed;
        public IEnumerable<DrawableImportResolveItem> VisibleItems => ShowResolvedItems
            ? Items
            : Items.Where(x => !x.IsResolved);
        public string HeaderText
        {
            get
            {
                var unresolvedCount = Items.Count(x => !x.IsResolved);
                return unresolvedCount == 1
                    ? "Resolve 1 drawable before import"
                    : $"Resolve {unresolvedCount} drawables before import";
            }
        }
        public string HelpText
        {
            get
            {
                var parts = new List<string>();
                if (_showGender)
                {
                    parts.Add("gender");
                }

                if (_showDrawableProperties)
                {
                    parts.Add("drawable properties");
                }

                return $"Click rows to check them, then apply {string.Join(" and ", parts)} to checked files. Resolved files are hidden unless Show resolved is enabled.";
            }
        }
        public string SelectionSummary => $"{Items.Count(x => x.IsSelected)} checked, {Items.Count(x => x.IsResolved)} of {Items.Count} resolved";

        private bool _showResolvedItems;
        public bool ShowResolvedItems
        {
            get => _showResolvedItems;
            set
            {
                if (_showResolvedItems != value)
                {
                    _showResolvedItems = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VisibleItems));
                }
            }
        }

        private string _selectedGender;
        public string SelectedGender
        {
            get => _selectedGender;
            set
            {
                if (_selectedGender != value)
                {
                    _selectedGender = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsGenderApplyEnabled));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
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
                    OnPropertyChanged(nameof(IsDrawableApplyEnabled));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        public bool IsProp => SelectedAssetType == "Prop";
        public bool IsAssetTypeSelected => DrawableTypes.Count > 0;
        public bool IsGenderApplyEnabled => SelectedGender != null && GetControlTargetItems().Any(x => x.RequiresGender);
        public bool IsDrawableApplyEnabled => SelectedDrawableType != null && GetControlTargetItems().Any(x => x.RequiresDrawableType);
        public bool IsSubmitEnabled => Items.Count > 0 && (Items.All(x => x.IsResolved) || CanResolveSingleUnresolvedItem());

        private List<string> _drawableTypes = [];
        public List<string> DrawableTypes
        {
            get => _drawableTypes;
            set
            {
                if (_drawableTypes != value)
                {
                    _drawableTypes = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAssetTypeSelected));
                    OnPropertyChanged(nameof(IsDrawableApplyEnabled));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        private string _selectedDrawableType;
        public string SelectedDrawableType
        {
            get => _selectedDrawableType;
            set
            {
                if (_selectedDrawableType != value)
                {
                    _selectedDrawableType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDrawableApplyEnabled));
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        public DrawableImportResolveWindow(
            IEnumerable<string> paths,
            Dictionary<string, Enums.SexType?> detectedGenders = null,
            Dictionary<string, (bool IsProp, int DrawableType)?> detectedDrawableTypes = null,
            bool showGender = false,
            bool showDrawableProperties = true)
        {
            InitializeComponent();

            _showGender = showGender;
            _showDrawableProperties = showDrawableProperties;

            foreach (var path in paths)
            {
                Enums.SexType? gender = null;
                (bool IsProp, int DrawableType)? drawableType = null;
                detectedGenders?.TryGetValue(path, out gender);
                detectedDrawableTypes?.TryGetValue(path, out drawableType);

                var item = new DrawableImportResolveItem(
                    path,
                    gender,
                    drawableType,
                    _showGender,
                    _showDrawableProperties);
                item.PropertyChanged += ImportItem_PropertyChanged;
                Items.Add(item);
            }

            DataContext = this;
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
            SetDrawableTypeOptions(SelectedAssetType);
        }

        private void ApplyGender_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            if (SelectedGender == null)
            {
                return;
            }

            var gender = SelectedGender == "Male" ? Enums.SexType.male : Enums.SexType.female;
            var targetItems = GetControlTargetItems()
                .Where(x => x.RequiresGender)
                .ToList();

            _isApplyingBulkUpdate = true;
            foreach (var item in targetItems)
            {
                item.SetGender(gender);
            }

            foreach (var item in targetItems)
            {
                item.IsSelected = !item.IsResolved;
            }
            _isApplyingBulkUpdate = false;

            OnItemsChanged();
        }

        private void ApplyDrawable_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            if (SelectedDrawableType == null)
            {
                return;
            }

            var isProp = IsProp;
            var drawableType = SelectedDrawableType;
            var targetItems = GetControlTargetItems()
                .Where(x => x.RequiresDrawableType)
                .ToList();

            _isApplyingBulkUpdate = true;
            foreach (var item in targetItems)
            {
                item.SetDrawableType(isProp, drawableType);
            }

            foreach (var item in targetItems)
            {
                item.IsSelected = !item.IsResolved;
            }
            _isApplyingBulkUpdate = false;

            OnItemsChanged();
        }

        private void Select_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            ResolveSingleUnresolvedItemIfPossible();

            if (!IsSubmitEnabled)
            {
                return;
            }

            if (_showGender)
            {
                SelectedGenders = Items.ToDictionary(
                    item => item.FilePath,
                    item => item.Gender.GetValueOrDefault());
            }

            if (_showDrawableProperties)
            {
                SelectedDrawableTypes = Items.ToDictionary(
                    item => item.FilePath,
                    item => (item.IsProp == true, item.DrawableType.GetValueOrDefault()));
            }

            DialogResult = true;
            Close();
        }

        private bool CanResolveSingleUnresolvedItem()
        {
            var unresolvedItems = Items.Where(x => !x.IsResolved).ToList();
            if (unresolvedItems.Count != 1)
            {
                return false;
            }

            var item = unresolvedItems[0];
            var canResolveGender = !item.RequiresGender || item.HasGender || SelectedGender != null;
            var canResolveDrawableType = !item.RequiresDrawableType || item.HasDrawableType || SelectedDrawableType != null;

            return canResolveGender && canResolveDrawableType;
        }

        private void ResolveSingleUnresolvedItemIfPossible()
        {
            if (!CanResolveSingleUnresolvedItem())
            {
                return;
            }

            var item = Items.First(x => !x.IsResolved);

            if (item.RequiresGender && !item.HasGender && SelectedGender != null)
            {
                var gender = SelectedGender == "Male" ? Enums.SexType.male : Enums.SexType.female;
                item.SetGender(gender);
            }

            if (item.RequiresDrawableType && !item.HasDrawableType && SelectedDrawableType != null)
            {
                item.SetDrawableType(IsProp, SelectedDrawableType);
            }

            item.IsSelected = !item.IsResolved;
            OnItemsChanged();
        }

        private void Cancel_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            var visibleItems = VisibleItems.ToHashSet();

            _isApplyingBulkUpdate = true;
            foreach (var item in Items)
            {
                item.IsSelected = visibleItems.Contains(item);
            }
            _isApplyingBulkUpdate = false;

            OnItemsChanged();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in VisibleItems)
            {
                item.IsSelected = false;
            }

            OnItemsChanged();
        }

        private void ImportItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrawableImportResolveItem.IsSelected) ||
                e.PropertyName == nameof(DrawableImportResolveItem.IsResolved))
            {
                OnItemsChanged();
            }
        }

        private void ImportItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DrawableImportResolveItem item })
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        private void OnItemsChanged()
        {
            if (!_isApplyingBulkUpdate)
            {
                SyncControlSelectionsFromSingleTarget();
            }

            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(VisibleItems));
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(IsGenderApplyEnabled));
            OnPropertyChanged(nameof(IsDrawableApplyEnabled));
            OnPropertyChanged(nameof(IsSubmitEnabled));
            OnPropertyChanged(nameof(GenderControlsVisibility));
            OnPropertyChanged(nameof(DrawableControlsVisibility));
        }

        private List<DrawableImportResolveItem> GetControlTargetItems()
        {
            var selectedItems = Items.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count > 0)
            {
                return selectedItems;
            }

            var unresolvedItems = Items.Where(x => !x.IsResolved).ToList();
            return unresolvedItems.Count == 1 ? unresolvedItems : [];
        }

        private bool ShouldShowGenderControls()
        {
            return _showGender;
        }

        private bool ShouldShowDrawableControls()
        {
            return _showDrawableProperties;
        }

        private void SetDrawableTypeOptions(string assetType)
        {
            DrawableTypes = assetType switch
            {
                "Component" => EnumHelper.GetDrawableTypeList(),
                "Prop" => EnumHelper.GetPropTypeList(),
                _ => [],
            };
        }

        private void SyncControlSelectionsFromSingleTarget()
        {
            var targetItems = GetControlTargetItems();
            if (targetItems.Count != 1)
            {
                return;
            }

            var item = targetItems[0];
            if (_showGender && item.HasGender)
            {
                SelectedGender = DrawableImportResolveItem.GetGenderDisplayName(item.Gender.Value);
            }

            if (_showDrawableProperties && item.HasDrawableType)
            {
                var assetType = item.IsProp == true ? "Prop" : "Component";
                SelectedAssetType = assetType;
                SetDrawableTypeOptions(assetType);
                SelectedDrawableType = item.DrawableTypeName;
            }
        }

        private static bool NeedsGender(DrawableImportResolveItem item)
        {
            return item.RequiresGender && !item.HasGender;
        }

        private static bool NeedsDrawableType(DrawableImportResolveItem item)
        {
            return item.RequiresDrawableType && !item.HasDrawableType;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
