using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        // Field that will temporarily store result before we return it and close CustomMessageBox
        private static CustomMessageBoxResult result = CustomMessageBoxResult.OK;
        public string TextBoxValue => CMBTextBox.Text;

        // Buttons defined as properties, because couldn't be created (initialized) with event subscription at same time "on-the-fly".
        // You can add new different buttons by adding new one as property here
        // and to CustomMessageBoxButtons and CustomMessageBoxResult enums   
        private Button OK
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = nameof(OK);
                b.Click += delegate { result = CustomMessageBoxResult.OK; Close(); };
                return b;
            }
        }
        private Button Cancel
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = nameof(Cancel);
                b.Click += delegate { result = CustomMessageBoxResult.Cancel; Close(); };
                return b;
            }
        }
        private Button Yes
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = nameof(Yes);
                b.Click += delegate { result = CustomMessageBoxResult.Yes; Close(); };
                return b;
            }
        }
        private Button No
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = nameof(No);
                b.Click += delegate { result = CustomMessageBoxResult.No; Close(); };
                return b;
            }
        }
        private Button OpenFolder
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = "Open Folder";
                b.Click += delegate { result = CustomMessageBoxResult.OpenFolder; Close(); };
                return b;
            }
        }

        private Button Delete
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = "Delete";
                b.Click += delegate { result = CustomMessageBoxResult.Delete; Close(); };
                return b;
            }
        }

        private Button Replace
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = "Replace";
                b.Click += delegate { result = CustomMessageBoxResult.Replace; Close(); };
                return b;
            }
        }

        private Button Male
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = "Male";
                b.Click += delegate { result = CustomMessageBoxResult.Male; Close(); };
                return b;
            }
        }

        private Button Female
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = "Female";
                b.Click += delegate { result = CustomMessageBoxResult.Female; Close(); };
                return b;
            }
        }
        // Add another if you wish


        // There is no empty constructor. As least "message" should be passed to this CustomMessageBox
        // Also constructor is private to prevent create its instances somewhere and force to use only static Show methods
        private CustomMessageBox(string message,
                                 string caption = "",
                                 CustomMessageBoxButtons cmbButtons = CustomMessageBoxButtons.OKOnly,
                                 CustomMessageBoxIcon cmbIcon = CustomMessageBoxIcon.None,
                                 string path = "",
                                 bool showTextBox = false)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            // Handle Ctrl+C press to copy message from CustomMessageBox
            KeyDown += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C))
                    Clipboard.SetText(CMBMessage.Text);
            };

            // Set message
            CMBMessage.Text = message;
            // Set caption
            CMBCaption.Text = caption;
            CMBTextBox.Text = "";
            CMBTextBox.Visibility = showTextBox ? Visibility.Visible : Visibility.Collapsed;

            // Setup Buttons (depending on specified CustomMessageBoxButtons value)
            // As StackPanel FlowDirection set as RightToLeft - we should add items in reverse
            switch (cmbButtons)
            {
                case CustomMessageBoxButtons.OKOnly:
                    _ = CMBButtons.Children.Add(OK);
                    break;
                case CustomMessageBoxButtons.OKCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(OK);
                    break;
                case CustomMessageBoxButtons.YesNo:
                    _ = CMBButtons.Children.Add(No);
                    _ = CMBButtons.Children.Add(Yes);
                    break;
                case CustomMessageBoxButtons.YesNoCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(No);
                    _ = CMBButtons.Children.Add(Yes);
                    break;
                case CustomMessageBoxButtons.OpenFolder:
                    _ = CMBButtons.Children.Add(OK);
                    _ = CMBButtons.Children.Add(OpenFolder);
                    break;
                case CustomMessageBoxButtons.DeleteReplaceCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(Replace);
                    _ = CMBButtons.Children.Add(Delete);
                    break;
                case CustomMessageBoxButtons.MaleFemaleCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(Female);
                    _ = CMBButtons.Children.Add(Male);
                    break;
                // Add another if you wish                 
                default:
                    _ = CMBButtons.Children.Add(OK);
                    break;
            }

            // Set icon (depending on specified CustomMessageBoxIcon value)
            // From C# 8.0 could be converted to switch-expression
            switch (cmbIcon)
            {
                case CustomMessageBoxIcon.Information:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Information);
                    break;
                case CustomMessageBoxIcon.Warning:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Warning);
                    break;
                case CustomMessageBoxIcon.Question:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Question);
                    break;
                case CustomMessageBoxIcon.Error:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Error);
                    break;
                case CustomMessageBoxIcon.None:
                default:
                    CMBIcon.Source = null;
                    break;
            }
        }

        // Show methods create new instance of CustomMessageBox window and shows it as Dialog (blocking thread)

        // Shows CustomMessageBox with specified message and default "OK" button
        public static CustomMessageBoxResult Show(string message)
        {
            _ = new CustomMessageBox(message).ShowDialog();
            return result;
        }

        // Shows CustomMessageBox with specified message, caption and default "OK" button
        public static CustomMessageBoxResult Show(string message, string caption)
        {
            _ = new CustomMessageBox(message, caption).ShowDialog();
            return result;
        }

        // Shows CustomMessageBox with specified message, caption and button(s)
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons)
        {
            _ = new CustomMessageBox(message, caption, cmbButtons).ShowDialog();
            return result;
        }

        // Shows CustomMessageBox with specified message, caption and button(s) and path
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons, string path)
        {
            _ = new CustomMessageBox(message, caption, cmbButtons, path: path).ShowDialog();

            if (result == CustomMessageBoxResult.OpenFolder)
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            return result;
        }

        // Shows CustomMessageBox with specified message, caption, button(s) and icon.
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons, CustomMessageBoxIcon cmbIcon)
        {
            _ = new CustomMessageBox(message, caption, cmbButtons, cmbIcon).ShowDialog();
            return result;
        }

        public static (CustomMessageBoxResult result, string textBoxValue) Show(string message, string caption, CustomMessageBoxButtons cmbButtons, CustomMessageBoxIcon cmbIcon, bool showTextBox)
        {
            var customMessageBox = new CustomMessageBox(message, caption, cmbButtons, cmbIcon, showTextBox: showTextBox);
            customMessageBox.ShowDialog();

            // If the TextBox is visible, return its value along with the button result
            if (showTextBox)
            {
                return (result, customMessageBox.TextBoxValue);
            }

            return (result, null);
        }


        // Defines button(s), which should be displayed
        public enum CustomMessageBoxButtons
        {
            OKOnly,
            OKCancel,
            YesNo,
            YesNoCancel,
            OpenFolder,
            DeleteReplaceCancel,
            MaleFemaleCancel

            // Add another if you wish
        }

        // Defines icon, which should be displayed
        public enum CustomMessageBoxIcon
        {
            None,
            Question,
            Information,
            Warning,
            Error
        }

        // Defines button, pressed by user as result
        public enum CustomMessageBoxResult
        {
            OK,
            Cancel,
            Yes,
            No,
            OpenFolder,
            Delete,
            Replace,
            TextBoxValue,
            Male,
            Female


            // Add another if you wish
        }


        // Returns simple Button with pre-defined properties
        private static Button GetDefaultButton() => new Button
        {
            Width = 72,
            Height = 28,
            Margin = new Thickness(0, 4, 6, 4),
            Background = Brushes.White,
            BorderBrush = Brushes.DarkGray,
            Foreground = Brushes.Black
        };

        // Converts system icons (like in original message box) to BitmapSource to be able to set it to Source property of Image control 
        private static BitmapSource FromSystemIcon(Icon icon) =>
            Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        // Handler on CustomMessageBox caption-header to allow move window while left button pressed on it
        private void OnCaptionPress(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}

