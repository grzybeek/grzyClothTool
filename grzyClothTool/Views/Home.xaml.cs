using System.Windows;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Window
    {
        public Home()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //todo: window for selecting name when starting new addon
            MainWindow mainWindow = new();
            mainWindow.Show();
            this.Close();
        }
    }
}
