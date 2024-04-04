using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public ObservableCollection<LogMessage> LogMessages { get; set; } = [];

        public LogWindow()
        {
            InitializeComponent();
            Closing += LogWindow_Closing;
            DataContext = this;
        }

        public void LogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public class LogMessage
    {
        public string Timestamp { get; set; }
        public string Message { get; set; }

        public string TypeIcon { get; set; }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error
    }
}
