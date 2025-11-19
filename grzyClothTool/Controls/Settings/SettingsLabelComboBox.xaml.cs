using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for SettingsLabelComboBox.xaml
    /// </summary>
    public partial class SettingsLabelComboBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
            .Register("Label",
                    typeof(string),
                    typeof(SettingsLabelComboBox),
                    new FrameworkPropertyMetadata("Unnamed Label"));

        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty
            .Register("LabelFontSize",
                    typeof(int),
                    typeof(SettingsLabelComboBox),
                    new FrameworkPropertyMetadata(15));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty
            .Register("Description",
                    typeof(string),
                    typeof(SettingsLabelComboBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
            .Register("ItemsSource",
                    typeof(IEnumerable),
                    typeof(SettingsLabelComboBox),
                    new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
            .Register("SelectedItem",
                    typeof(object),
                    typeof(SettingsLabelComboBox),
                    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public SettingsLabelComboBox()
        {
            InitializeComponent();
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public int LabelFontSize
        {
            get { return (int)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
    }
}
