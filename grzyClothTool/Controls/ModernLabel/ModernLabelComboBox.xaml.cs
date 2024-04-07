using System.Collections;
using System.Windows;

namespace grzyClothTool.Controls
{
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
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty
            .Register("SelectedItem",
                    typeof(object),
                    typeof(ModernLabelComboBox),
                    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty
            .Register("ItemsSource",
                    typeof(IEnumerable),
                    typeof(ModernLabelComboBox),
                    new PropertyMetadata(null));

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

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
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
        }
    }
}