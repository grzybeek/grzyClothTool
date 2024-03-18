using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Timers;
using Application = System.Windows.Application;

namespace grzyClothTool.Views
{
    public interface ISplashScreen
    {
        void AddMessage(string message);
        Task LoadComplete();
        int MessageQueueCount { get; }
        void Shutdown();
    }

    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window, ISplashScreen
    {
        private readonly Queue<string> messageQueue = new();
        private readonly Timer messageTimer;

        public int MessageQueueCount
        {
            get { return messageQueue.Count; }
        }

        public SplashScreen()
        {
            InitializeComponent();

            messageTimer = new Timer(1500);
            messageTimer.Elapsed += ProcessMessageQueue;
            messageTimer.Start();
        }

        public void AddMessage(string message)
        {
            messageQueue.Enqueue(message);
        }

        private void ProcessMessageQueue(object sender, ElapsedEventArgs e)
        {
            if (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                Dispatcher.Invoke(() =>
                {
                    updateTextBox.Text = message;
                });
            }
        }

        public async Task LoadComplete()
        {
            messageTimer.Stop();

            Dispatcher.InvokeShutdown();
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            // for some weird reason this doesn't work without delay (main window is opened in background, not at the top)
            await Task.Delay(250);
            Application.Current.MainWindow.Activate();
        }

        public void Shutdown()
        {
            messageTimer.Stop();
            Dispatcher.InvokeShutdown();
        }
    }
}

