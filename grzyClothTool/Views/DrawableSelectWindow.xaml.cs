using grzyClothTool.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for DrawableSelectWindow.xaml
    /// </summary>
    public partial class DrawableSelectWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string AssetPath { get; set; }
        public List<string> AssetTypes { get; set; } = ["Component", "Prop"];
        public string SelectedAssetType { get; set; }
        public bool IsAssetTypeSelected => DrawableTypes.Count > 0;
        public bool IsSubmitEnabled => SelectedDrawableType != null;
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
                    OnPropertyChanged(nameof(IsSubmitEnabled));
                }
            }
        }

        public DrawableSelectWindow(string path)
        {
            InitializeComponent();
            DataContext = this;
            AssetPath = path;
        }

        private void AssetType_IsUpdated(object sender, Controls.UpdatedEventArgs e)
        {
            var newValue = e.DependencyPropertyChangedEventArgs.NewValue;
            if (newValue == null)
            {
                return;
            }

            DrawableTypes = newValue switch
            {
                "Component" => EnumHelper.GetDrawableTypeList(),
                "Prop" => EnumHelper.GetPropTypeList(),
                _ => [],
            };
        }

        private void Select_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Cancel_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
