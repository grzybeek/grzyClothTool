using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Navigation;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for AccountsWindow.xaml
    /// </summary>
    public partial class AccountsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string IsLoggedInText => IsPatreonLoggedIn ? "Yes" : "No";

        private bool _isPatreonLoggedIn;
        public bool IsPatreonLoggedIn
        {
            get { return _isPatreonLoggedIn; }
            set
            {
                if (_isPatreonLoggedIn != value)
                {
                    _isPatreonLoggedIn = value;
                    OnPropertyChanged(nameof(IsPatreonLoggedIn));
                }
            }
        }

        private string _patreonUsername;
        public string PatreonUsername
        {
            get { return _patreonUsername; }
            set
            {
                if (_patreonUsername != value)
                {
                    _patreonUsername = value;
                    OnPropertyChanged(nameof(PatreonUsername));
                }
            }
        }

        private string _patreonImg;
        public string PatreonImg
        {
            get { return _patreonImg; }
            set
            {
                if (_patreonImg != value)
                {
                    _patreonImg = value;
                    OnPropertyChanged(nameof(PatreonImg));
                }
            }
        }

        private string _patreonStatus;
        public string PatreonStatus
        {
            get { return _patreonStatus; }
            set
            {
                if (_patreonStatus != value)
                {
                    _patreonStatus = value;
                    OnPropertyChanged(nameof(PatreonStatus));
                }
            }
        }

        private string _patreonLastChargeDate;
        public string PatreonLastChargeDate
        {
            get { return _patreonLastChargeDate; }
            set
            {
                if (_patreonLastChargeDate != value)
                {
                    _patreonLastChargeDate = value;
                    OnPropertyChanged(nameof(PatreonLastChargeDate));
                }
            }
        }

        private string _patreonNextChargeDate;
        public string PatreonNextChargeDate
        {
            get { return _patreonNextChargeDate; }
            set
            {
                if (_patreonNextChargeDate != value)
                {
                    _patreonNextChargeDate = value;
                    OnPropertyChanged(nameof(PatreonNextChargeDate));
                }
            }
        }

        public AccountsWindow()
        {
            InitializeComponent();
            DataContext = this;

            try
            {
                var isLogged = App.patreonAuthPlugin?.IsLoggedIn;
                if (isLogged.HasValue && isLogged.Value)
                {
                    IsPatreonLoggedIn = true;
                    PatreonUsername = App.patreonAuthPlugin?.Username;
                    PatreonImg = App.patreonAuthPlugin?.ImageUrl;

                    PatreonStatus = App.patreonAuthPlugin?.Status == null ? "NOT ACTIVE" : "ACTIVE";
                    PatreonLastChargeDate = (App.patreonAuthPlugin?.LastChargeDate) ?? "-";
                    PatreonNextChargeDate = (App.patreonAuthPlugin?.NextChargeDate) ?? "-";
                }
            } 
            catch
            {
                Close();
                throw new Exception("Error while initializing AccountsWindow, most likely missing files. Report it please");
            }
        }

        private async void LoginPatreon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.patreonAuthPlugin?.Login();

                var isLogged = App.patreonAuthPlugin?.IsLoggedIn;
                if (isLogged.HasValue && isLogged.Value)
                {
                    IsPatreonLoggedIn = true;
                    PatreonUsername = App.patreonAuthPlugin.Username;
                    PatreonImg = App.patreonAuthPlugin.ImageUrl;

                    PatreonStatus = App.patreonAuthPlugin.Status == null ? "NOT ACTIVE" : "ACTIVE";
                    PatreonLastChargeDate = (App.patreonAuthPlugin.LastChargeDate) ?? "-";
                    PatreonNextChargeDate = (App.patreonAuthPlugin.NextChargeDate) ?? "-";
                }
            }
            catch
            {
                throw new Exception("Error while logging in, most likely missing files. Report it please");
            }
        }

        private void LogoutPatreon_Click(object sender, RoutedEventArgs e)
        {
            App.patreonAuthPlugin?.Logout();
            IsPatreonLoggedIn = false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process p = new();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = e.Uri.AbsoluteUri;
            p.Start();
        }
    }
}
