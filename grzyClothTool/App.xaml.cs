using grzyClothTool.Controls;
using System;
using System.IO;
using System.Windows;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;


            CustomMessageBox.Show($"An error occurred: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly);
            // Log the exception, e.g. to a file:
            File.WriteAllText("error.log", ex.ToString());

            // Or print the exception to the console:
            Console.WriteLine("Unhandled exception: " + ex.ToString());
        }
    }
}
