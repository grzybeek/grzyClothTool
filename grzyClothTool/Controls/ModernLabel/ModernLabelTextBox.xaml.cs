using System.Windows;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for ModernLabelTextBox.xaml
    /// </summary>
    public partial class ModernLabelTextBox : ModernLabelBaseControl
    {
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

        static ModernLabelTextBox()
        {
            FontSizeProperty.OverrideMetadata(
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(14.0));
        }

        public ModernLabelTextBox()
        {
            InitializeComponent();
            MyText.GotFocus += (s, e) => IsUserInitiated = true;
            MyText.LostFocus += (s, e) => IsUserInitiated = false;
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
