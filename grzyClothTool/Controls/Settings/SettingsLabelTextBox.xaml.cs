using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for LabelTextBox.xaml
    /// </summary>
    public partial class SettingsLabelTextBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
        .Register("Label",
                typeof(string),
                typeof(SettingsLabelTextBox),
                new FrameworkPropertyMetadata("Unnamed Label"));

        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty
            .Register("LabelFontSize",
                    typeof(int),
                    typeof(SettingsLabelTextBox),
                    new FrameworkPropertyMetadata(15));

        public static readonly DependencyProperty TextProperty = DependencyProperty
            .Register("Text",
                    typeof(string),
                    typeof(SettingsLabelTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty
            .Register("Description",
                    typeof(string),
                    typeof(SettingsLabelTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty TitleProperty = DependencyProperty
            .Register("Title",
                    typeof(string),
                    typeof(SettingsLabelTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly RoutedEvent ButtonClickEvent = EventManager.RegisterRoutedEvent("ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SettingsLabelTextBox));

        public static readonly DependencyProperty ButtonVisibleProperty = DependencyProperty
            .Register("ButtonVisible",
                    typeof(bool),
                    typeof(SettingsLabelTextBox),
                    new FrameworkPropertyMetadata(true));

        public bool ButtonVisible
        {
            get { return (bool)GetValue(ButtonVisibleProperty); }
            set { SetValue(ButtonVisibleProperty, value); }
        }

        public SettingsLabelTextBox()
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

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private bool _isButtonClickHandlerAttached;
        public event RoutedEventHandler ButtonClick
        {
            add { AddHandler(ButtonClickEvent, value); }
            remove { RemoveHandler(ButtonClickEvent, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ButtonClickEvent, this));
        }
    }
}
