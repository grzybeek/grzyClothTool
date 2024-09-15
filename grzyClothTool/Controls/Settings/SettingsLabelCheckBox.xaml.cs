using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Controls
{
    public class CheckBoxClickEventArgs : RoutedEventArgs
    {
        public bool IsChecked { get; set; }
        public CheckBoxClickEventArgs(RoutedEvent routedEvent, object source, bool isChecked)
            : base(routedEvent, source)
        {
            IsChecked = isChecked;
        }
    }

    public partial class SettingsLabelCheckBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
        .Register("Label",
                typeof(string),
                typeof(SettingsLabelCheckBox),
                new FrameworkPropertyMetadata("Unnamed Label"));

        public static readonly DependencyProperty TextProperty = DependencyProperty
            .Register("Text",
                    typeof(string),
                    typeof(SettingsLabelCheckBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty TitleProperty = DependencyProperty
            .Register("Title",
                    typeof(string),
                    typeof(SettingsLabelCheckBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty
            .Register("IsChecked",
            typeof(bool),
            typeof(SettingsLabelCheckBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DisplayWarningProperty = DependencyProperty
            .Register("DisplayWarning",
            typeof(string),
            typeof(SettingsLabelCheckBox),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly RoutedEvent CheckBoxClickEvent = EventManager.RegisterRoutedEvent("CheckBoxClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SettingsLabelCheckBox));


        public SettingsLabelCheckBox()
        {
            InitializeComponent();
        }

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

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public string DisplayWarning
        {
            get { return (string)GetValue(DisplayWarningProperty); }
            set { SetValue(DisplayWarningProperty, value); }
        }

        public event RoutedEventHandler CheckBoxClick
        {
            add { AddHandler(CheckBoxClickEvent, value); }
            remove { RemoveHandler(CheckBoxClickEvent, value); }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            bool isChecked = checkBox.IsChecked == true;

            if (isChecked && !string.IsNullOrEmpty(DisplayWarning))
            {
                CustomMessageBoxResult result = Show(DisplayWarning, "Warning", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.Warning);
                if (result == CustomMessageBoxResult.OK)
                {
                    checkBox.IsChecked = true;
                }
                else
                {
                    checkBox.IsChecked = false;
                }
            }

            RaiseEvent(new CheckBoxClickEventArgs(CheckBoxClickEvent, this, checkBox.IsChecked == true));
        }

    }
}
