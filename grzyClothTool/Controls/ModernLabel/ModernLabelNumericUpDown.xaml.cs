using System.Windows;

namespace grzyClothTool.Controls
{
    public partial class ModernLabelNumericUpDown : ModernLabelBaseControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
            .Register("Label", typeof(string), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata("Numeric UpDown"));

        public static readonly DependencyProperty ValueProperty = DependencyProperty
            .Register("Value", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M, OnUpdate));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty
            .Register("Minimum", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty
            .Register("Maximum", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(0.0M));

        public static readonly DependencyProperty IncrementProperty = DependencyProperty
            .Register("Increment", typeof(decimal), typeof(ModernLabelNumericUpDown), new FrameworkPropertyMetadata(1.0M));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public decimal Minimum
        {
            get { return (decimal)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public decimal Maximum
        {
            get { return (decimal)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public decimal Increment
        {
            get { return (decimal)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        public ModernLabelNumericUpDown()
        {
            InitializeComponent();
        }

        private void IncrementValue(object sender, RoutedEventArgs e)
        {
            if (Value + Increment <= Maximum)
            {
                IsUserInitiated = true;
                Value += Increment;
            }
        }

        private void DecrementValue(object sender, RoutedEventArgs e)
        {
            if (Value - Increment >= Minimum)
            {
                IsUserInitiated = true;
                Value -= Increment;
            }
        }
    }
}