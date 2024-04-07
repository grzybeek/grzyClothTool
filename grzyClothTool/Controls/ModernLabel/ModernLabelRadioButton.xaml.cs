using System.Windows;

namespace grzyClothTool.Controls
{
    public partial class ModernLabelRadioButton : ModernLabelBaseControl
    {
        public ModernLabelRadioButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register("GroupName", typeof(string), typeof(ModernLabelRadioButton), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ModernLabelRadioButton), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool?), typeof(ModernLabelRadioButton), new PropertyMetadata(false));

        public static readonly RoutedEvent RadioBtnSelectEvent = EventManager.RegisterRoutedEvent(
            "BtnSelectEvent",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ModernLabelRadioButton)
        );

        public event RoutedEventHandler MyBtnSelectEvent
        {
            add { AddHandler(RadioBtnSelectEvent, value); }
            remove { RemoveHandler(RadioBtnSelectEvent, value); }
        }

        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); }
        }

        public bool? IsChecked
        {
            get { return (bool?)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        private void RadioButton_Change(object sender, RoutedEventArgs e)
        {
            BtnSelectEventArgs args = new(RadioBtnSelectEvent);
            RaiseEvent(args);
        }

        private class BtnSelectEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
        {
        }
    }
}