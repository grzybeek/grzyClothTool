using grzyClothTool.Models;
using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for ModernLabelComboBox.xaml
    /// </summary>
    public partial class ModernLabelComboBox : UserControl
    {
        public event EventHandler<DependencyPropertyChangedEventArgs> IsUpdated;

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
        }

        private static void OnUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernLabelComboBox)d;
            control.IsUpdated?.Invoke(control, e);
        }
    }
}