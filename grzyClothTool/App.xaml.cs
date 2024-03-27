using grzyClothTool.Controls;
using grzyClothTool.Views;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ISplashScreen splashScreen;
        private ManualResetEvent ResetSplashCreated;
        private Thread SplashThread;

        protected override void OnStartup(StartupEventArgs e)
        {
            ResetSplashCreated = new ManualResetEvent(false);

            // Create a new thread for the splash screen to run on
            SplashThread = new Thread(ShowSplash);
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.Start();

            ResetSplashCreated.WaitOne();
            base.OnStartup(e);
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;


            Show($"An error occurred: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly);

            File.WriteAllText("error.log", ex.ToString());

            Console.WriteLine("Unhandled exception: " + ex.ToString());

            if (e.IsTerminating)
            {
                //todo: save
                Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }

        private void ShowSplash()
        {
            Views.SplashScreen animatedSplashScreenWindow = new();
            splashScreen = animatedSplashScreenWindow;

            animatedSplashScreenWindow.Show();

            ResetSplashCreated.Set();
            System.Windows.Threading.Dispatcher.Run();
        }

    }
}
