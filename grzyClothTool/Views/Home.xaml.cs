using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private List<string> _patreonList;
        public List<string> PatreonList
        {
            get => _patreonList;
            set
            {
                _patreonList = value;
                OnPropertyChanged(nameof(PatreonList));
            }
        }

        private List<string> _ghSponsorsList;
        public List<string> GhSponsorsList
        {
            get => _ghSponsorsList;
            set
            {
                _ghSponsorsList = value;
                OnPropertyChanged(nameof(GhSponsorsList));
            }
        }

        private string _changelog;
        public string Changelog
        {
            get => _changelog;
            set
            {
                _changelog = value;
                OnPropertyChanged(nameof(Changelog));
            }
        }


        private readonly List<string> didYouKnowStrings = [
            "You can open any existing addon and it will load all properties such as heels or hats.",
            "You can export an existing project when you are not finished and later import it to continue working on it.",
            "There is switch to enable dark theme in the settings.",
            "There is 'live texture' feature in 3d preview? It allows you to see how your texture looks on the model in real time, even after changes.",
            "You can click SHIFT + DEL to instantly delete a selected drawable, without popup.",
            "You can click CTRL + DEL to instantly replace a selected drawable with reserved drawable.",
            "You can reserve your drawables and later change it to real model.",
            "Supporting me with monthly patreon will speed up the development of the tool!",
            "You can hover over warning icon to see what is wrong with your drawable or texture.",
        ];

        public string RandomDidYouKnow => didYouKnowStrings[new Random().Next(0, didYouKnowStrings.Count)];

        public Home()
        {
            InitializeComponent();
            DataContext = this;

            GhSponsorsList = [];
            Loaded += Home_Loaded;
        }

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await FetchPatreons();
            } 
            catch
            {
                PatreonList = ["Failed to fetch patreons"];
            }

            try
            {
                await FetchGithubSponsors();
            }
            catch
            {
                GhSponsorsList = ["Failed to fetch github sponsors"];
            }

            try
            {
                await FetchChangelog();
            }
            catch
            {
                ChangelogBrowser.NavigateToString("Failed to fetch changelog");
            }
        }

        private async Task FetchPatreons()
        {
            var url = "https://grzy.tools/grzyClothTool/patreons";

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                PatreonList = JsonSerializer.Deserialize<List<string>>(content);
            }
        }

        private async Task FetchGithubSponsors()
        {
            var url = "https://ghs.vercel.app/v3/sponsors/grzybeek";

            try
            {
                var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine("JSON Response: " + content); // Log the JSON response

                    GhSponsorsList = new List<string>();
                    using var jsonDocument = JsonDocument.Parse(content);

                    var currentSponsors = jsonDocument.RootElement.GetProperty("sponsors").GetProperty("current");
                    foreach (var sponsor in currentSponsors.EnumerateArray())
                    {
                        var username = sponsor.GetProperty("username").GetString();
                        if (!string.IsNullOrEmpty(username))
                        {
                            GhSponsorsList.Add(username);
                        }
                    }

                    OnPropertyChanged(nameof(GhSponsorsList));
                }
                else
                {
                    Console.WriteLine("Failed to fetch GitHub sponsors. Status code: " + response.StatusCode);
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("JSON Parsing Error: " + jsonEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private async Task FetchChangelog()
        {
            var url = "https://grzy.tools/grzyClothTool/changelog";

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() =>
                {
                    ChangelogBrowser.NavigateToString(content);
                });
            }
        }

        private void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            var (result, textBoxValue) = Show("Choose a name for your project", "Project Name", CustomMessageBoxButtons.OKCancel, CustomMessageBoxIcon.None, true);
            if (result == CustomMessageBoxResult.OK)
            {
                MainWindow.AddonManager.ProjectName = textBoxValue;
            }

            MainWindow.AddonManager.CreateAddon();
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.OpenAddonAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.ImportProjectAsync(true);
            MainWindow.NavigationHelper.Navigate("Project");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
