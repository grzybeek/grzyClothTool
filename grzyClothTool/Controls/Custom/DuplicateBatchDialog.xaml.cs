using grzyClothTool.Models.Drawable;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    public class DuplicateBatchItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public GDrawable Drawable { get; set; }
        public List<GDrawable> ExistingDuplicates { get; set; }

        private bool _shouldAdd = true;
        public bool ShouldAdd
        {
            get => _shouldAdd;
            set
            {
                if (_shouldAdd != value)
                {
                    _shouldAdd = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NewDrawableName => System.IO.Path.GetFileName(Drawable?.FilePath) ?? "Unknown";

        public string DuplicateOfText
        {
            get
            {
                if (ExistingDuplicates == null || ExistingDuplicates.Count == 0)
                    return "No existing duplicates found";

                var names = ExistingDuplicates.Select(d => d.Name).Take(3);
                var text = "Duplicate of: " + string.Join(", ", names);
                if (ExistingDuplicates.Count > 3)
                    text += $" (+{ExistingDuplicates.Count - 3} more)";
                return text;
            }
        }

        public string TypeDisplay => Drawable?.IsProp == true ? "Prop" : "Component";

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DuplicateBatchResult
    {
        public bool Cancelled { get; set; }
        public List<GDrawable> DrawablesToAdd { get; set; } = [];
        public List<GDrawable> DrawablesToSkip { get; set; } = [];
    }

    public partial class DuplicateBatchDialog : Window
    {
        private readonly List<DuplicateBatchItem> _items;
        private DuplicateBatchResult _result;

        public DuplicateBatchDialog(List<DuplicateBatchItem> duplicateItems)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            _items = duplicateItems;
            DuplicatesList.ItemsSource = _items;

            UpdateSummary();

            foreach (var item in _items)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(DuplicateBatchItem.ShouldAdd))
                        UpdateSummary();
                };
            }
        }

        private void UpdateSummary()
        {
            var selectedCount = _items.Count(x => x.ShouldAdd);
            SummaryText.Text = $"{selectedCount} of {_items.Count} duplicate(s) will be added";
        }

        public static DuplicateBatchResult Show(List<DuplicateBatchItem> duplicateItems)
        {
            var dialog = new DuplicateBatchDialog(duplicateItems);
            dialog.ShowDialog();
            return dialog._result ?? new DuplicateBatchResult { Cancelled = true };
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.ShouldAdd = true;
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
                item.ShouldAdd = false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _result = new DuplicateBatchResult
            {
                Cancelled = true,
                DrawablesToAdd = [],
                DrawablesToSkip = _items.Select(x => x.Drawable).ToList()
            };
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            _result = new DuplicateBatchResult
            {
                Cancelled = false,
                DrawablesToAdd = _items.Where(x => x.ShouldAdd).Select(x => x.Drawable).ToList(),
                DrawablesToSkip = _items.Where(x => !x.ShouldAdd).Select(x => x.Drawable).ToList()
            };
            Close();
        }

        private void OnCaptionPress(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
