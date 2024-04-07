using System;
using System.Windows;

namespace grzyClothTool.Controls
{
    public partial class ModernLabelCheckBox : ModernLabelBaseControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
            .Register("Label", typeof(string), typeof(ModernLabelCheckBox), new FrameworkPropertyMetadata("Placeholder"));

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty
            .Register("IsChecked", typeof(bool), typeof(ModernLabelCheckBox), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public ModernLabelCheckBox()
        {
            InitializeComponent();
            MyCheckBox.PreviewMouseDown += (s, e) => IsUserInitiated = true;
        }
    }
}