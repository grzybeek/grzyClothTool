using System.Windows;
using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for ModernLabelTextBox.xaml
    /// </summary>
    public partial class ModernLabelTextBox : ModernLabelBaseControl
    {
        private const string ProjectNameAllowedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789_";

        public enum TextFilterMode
        {
            None,
            ProjectName
        }

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

        public static readonly DependencyProperty IsFileSelectionProperty = DependencyProperty
            .Register("IsFileSelection",
                typeof(bool),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty FileExtensionProperty = DependencyProperty
            .Register("FileExtension",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty OriginalSelectedPathProperty = DependencyProperty
            .Register("OriginalSelectedPath",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty FilterModeProperty = DependencyProperty
            .Register("FilterMode",
                typeof(TextFilterMode),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(TextFilterMode.None));

        private bool _isUpdatingText;

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

        public bool IsFileSelection
        {
            get { return (bool)GetValue(IsFileSelectionProperty); }
            set { SetValue(IsFileSelectionProperty, value); }
        }

        public string FileExtension
        {
            get { return (string)GetValue(FileExtensionProperty); }
            set { SetValue(FileExtensionProperty, value); }
        }

        public string OriginalSelectedPath
        {
            get { return (string)GetValue(OriginalSelectedPathProperty); }
            set { SetValue(OriginalSelectedPathProperty, value); }
        }

        public TextFilterMode FilterMode
        {
            get { return (TextFilterMode)GetValue(FilterModeProperty); }
            set { SetValue(FilterModeProperty, value); }
        }

        public bool IsFolderOrFileSelection {
            get {
                return IsFolderSelection || IsFileSelection;
            } 
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
            DataObject.AddPastingHandler(MyText, MyText_Pasting);
        }

        private void MyText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsUserInitiated = true;
            if (IsFolderSelection)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.ShowDialog();

                OriginalSelectedPath = dialog.SelectedPath;
                Text = dialog.SelectedPath;
            } 
            else if (IsFileSelection)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = FileExtension,
                    Filter = $"{FileExtension} files (*{FileExtension})|*{FileExtension}"
                };
                dialog.ShowDialog();

                OriginalSelectedPath = dialog.FileName;
                Text = dialog.FileName;
            }
            IsUserInitiated = false;
        }

        private void MyText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!ShouldFilterText)
            {
                return;
            }

            var sanitizedText = SanitizeText(e.Text);

            if (sanitizedText == e.Text)
            {
                return;
            }

            e.Handled = true;
            InsertText(sanitizedText);
        }

        private void MyText_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!ShouldFilterText)
            {
                return;
            }

            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.Text) as string;
            var sanitizedText = SanitizeText(pastedText);

            if (sanitizedText == pastedText)
            {
                return;
            }

            e.CancelCommand();
            InsertText(sanitizedText);
        }

        private void MyText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || !ShouldFilterText)
            {
                return;
            }

            var sanitizedText = SanitizeText(MyText.Text);

            if (sanitizedText == MyText.Text)
            {
                return;
            }

            var selectionStart = MyText.SelectionStart;

            _isUpdatingText = true;
            MyText.Text = sanitizedText;
            MyText.SelectionStart = Math.Min(selectionStart, sanitizedText.Length);
            Text = sanitizedText;
            _isUpdatingText = false;
        }

        private void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var selectionStart = MyText.SelectionStart;

            MyText.SelectedText = text;
            MyText.SelectionStart = selectionStart + text.Length;
            MyText.SelectionLength = 0;
        }

        private string SanitizeText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (FilterMode == TextFilterMode.ProjectName)
            {
                value = value.ToLowerInvariant();
            }

            var allowedCharacters = GetAllowedCharacters(FilterMode);

            if (allowedCharacters == null)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (allowedCharacters.Contains(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private bool ShouldFilterText => FilterMode != TextFilterMode.None;

        private static string GetAllowedCharacters(TextFilterMode filterMode)
        {
            if (filterMode == TextFilterMode.ProjectName)
            {
                return ProjectNameAllowedCharacters;
            }

            return null;
        }
    }
}
