using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for Button.xaml
    /// </summary>
    public partial class CustomButton : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
        .Register("Label",
                typeof(string),
                typeof(CustomButton),
                new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty DropdownEnabledProperty = DependencyProperty
        .Register("DropdownEnabled",
                typeof(bool),
                typeof(CustomButton),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ImageProperty = DependencyProperty
            .Register("Image",
                typeof(string),
                typeof(CustomButton),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty TooltipProperty = DependencyProperty
            .Register("Tooltip",
                typeof(string),
                typeof(CustomButton),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty
        .Register("CornerRadius",
                typeof(CornerRadius),
                typeof(CustomButton),
                new FrameworkPropertyMetadata(new CornerRadius(0)));

        public new static readonly DependencyProperty FontSizeProperty = DependencyProperty
            .Register("FontSize", typeof(double), typeof(CustomButton), new FrameworkPropertyMetadata(16.0));

        public static readonly RoutedEvent BtnClickEvent = EventManager.RegisterRoutedEvent(
            "BtnClickEvent",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(CustomButton)
        );

        public event RoutedEventHandler MyBtnClickEvent
        {
            add { AddHandler(BtnClickEvent, value); }
            remove { RemoveHandler(BtnClickEvent, value); }
        }

        public object DropdownContent
        {
            get { return DropdownContentPresenter.Content; }
            set { DropdownContentPresenter.Content = value; }
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public string Image
        {
            get { return (string)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public string Tooltip
        {
            get { return (string)GetValue(TooltipProperty); }
            set { SetValue(TooltipProperty, value); }
        }

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public new double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public bool DropdownEnabled
        {
            get { return (bool)GetValue(DropdownEnabledProperty); }
            set { SetValue(DropdownEnabledProperty, value); }
        }

        public CustomButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = sender as Button;

            BtnClickEventArgs args = new(BtnClickEvent);
            RaiseEvent(args);
        }

        public class BtnClickEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
        {
        }
    }
}
