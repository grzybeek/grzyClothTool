using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for ModernLabelTextBox.xaml
    /// </summary>
    public partial class ModernLabelTextBox : UserControl
    {
        public event EventHandler IsUpdated;

        public static readonly DependencyProperty LabelProperty = DependencyProperty
        .Register("Label",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata("Placeholder"));

        public static readonly DependencyProperty TextProperty = DependencyProperty
            .Register("Text",
                    typeof(string),
                    typeof(ModernLabelTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public static readonly DependencyProperty IsFolderSelectionProperty = DependencyProperty
            .Register("IsFolderSelection",
                typeof(bool),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(false));

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

        public bool IsFolderSelection
        {
            get { return (bool)GetValue(IsFolderSelectionProperty); }
            set { SetValue(IsFolderSelectionProperty, value); }
        }   

        private static void OnUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ModernLabelTextBox)d;

            control.IsUpdated?.Invoke(control, EventArgs.Empty);
        }

        static ModernLabelTextBox()
        {
            FontSizeProperty.OverrideMetadata(
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(14.0));
        }

        public ModernLabelTextBox()
        {
            InitializeComponent();
        }

        private void MyText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(!IsFolderSelection) { return; }

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();

            Text = dialog.SelectedPath;
        }
    }
}
