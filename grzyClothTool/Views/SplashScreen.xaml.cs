using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window, ISplashScreen
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        public void AddMessage(string message)
        {
            Dispatcher.Invoke(delegate ()
            {
                updateTextBox.Text = message;
            });
        }

        public async void LoadComplete()
        {
            // for some weird reason this doesn't work without delay (main window is opened in background, not at the top)
            await Task.Delay(500);
            Dispatcher.InvokeShutdown();
            Application.Current.MainWindow.Activate();
        }
    }

    public interface ISplashScreen
    {
        void AddMessage(string message);
        void LoadComplete();
    }
}

