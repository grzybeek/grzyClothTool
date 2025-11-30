using grzyClothTool.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        private string _latestVersion;
        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged(nameof(LatestVersion));
            }
        }

        private List<string> _changelogHighlights;
        public List<string> ChangelogHighlights
        {
            get => _changelogHighlights;
            set
            {
                _changelogHighlights = value;
                OnPropertyChanged(nameof(ChangelogHighlights));
            }
        }

        private List<ToolInfo> _otherTools;
        public List<ToolInfo> OtherTools
        {
            get => _otherTools;
            set
            {
                _otherTools = value;
                OnPropertyChanged(nameof(OtherTools));
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

            OtherTools = [
                new ToolInfo
                {
                    Name = "grzyOptimizer",
                    Description = "Optimize YDD models, reduce polygon and vertex count while maintaining visual quality.",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                },
                new ToolInfo
                {
                    Name = "grzyTattooTool",
                    Description = "Create and edit tattoos with preview and quick addon resource generation for FiveM.",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                }
            ];

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
                await FetchLatestRelease();
            }
            catch
            {
                LatestVersion = "Unable to fetch version";
                ChangelogHighlights = ["Failed to load changelog highlights"];
            }
        }

        private async Task FetchPatreons()
        {
            var url = $"{GlobalConstants.GRZY_TOOLS_URL}/grzyClothTool/patreons";

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                PatreonList = JsonSerializer.Deserialize<List<string>>(content);
            }
        }

        private async Task FetchLatestRelease()
        {
            var url = "https://api.github.com/repos/grzybeek/grzyClothTool/releases/latest";

            App.httpClient.DefaultRequestHeaders.UserAgent.Clear();
            App.httpClient.DefaultRequestHeaders.Add("User-Agent", "grzyClothTool");

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var release = JsonSerializer.Deserialize<JsonElement>(content);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    if (release.TryGetProperty("tag_name", out var tagName))
                    {
                        LatestVersion = tagName.GetString();
                    }

                    if (release.TryGetProperty("body", out var body))
                    {
                        ChangelogHighlights = ParseChangelogHighlights(body.GetString());
                    }
                });
            }
        }

        private static List<string> ParseChangelogHighlights(string changelogBody)
        {
            if (string.IsNullOrWhiteSpace(changelogBody))
                return ["No changelog available"];

            var highlights = new List<string>();
            var lines = changelogBody.Split(['\r', '\n'], StringSplitOptions.None);
            var inChangelogSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Changelog", StringComparison.OrdinalIgnoreCase))
                {
                    inChangelogSection = true;
                    continue;
                }

                if (inChangelogSection && string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (inChangelogSection && trimmedLine.StartsWith("##"))
                {
                    break;
                }

                if (inChangelogSection &&
                    (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*") || trimmedLine.StartsWith("•")))
                {
                    var cleanLine = trimmedLine.TrimStart('-', '*', '•', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        highlights.Add(cleanLine);

                        if (highlights.Count >= 10)
                            break;
                    }
                }
            }

            return highlights.Count > 0 ? highlights : ["See full changelog for details"];
        }

        private void ViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/grzybeek/grzyClothTool/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open changelog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAllTools_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GlobalConstants.GRZY_TOOLS_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open website: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenToolUrl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

    public class ToolInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}
