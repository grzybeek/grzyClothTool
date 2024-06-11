using System.ComponentModel;

namespace grzyClothTool.Helpers;

public class SettingsHelper : INotifyPropertyChanged
{
    private static SettingsHelper _instance;
    public static SettingsHelper Instance => _instance ??= new();

    public event PropertyChangedEventHandler PropertyChanged;

    private bool _displaySelectedDrawablePath;
    public bool DisplaySelectedDrawablePath
    {
        get => _displaySelectedDrawablePath;
        set
        {
            if (_displaySelectedDrawablePath != value)
            {
                _displaySelectedDrawablePath = value;
                Properties.Settings.Default.DisplaySelectedDrawablePath = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(DisplaySelectedDrawablePath));
            }
        }
    }

    private int _polygonCountLimit;
    public int PolygonCountLimit
    {
        get => _polygonCountLimit;
        set
        {
            if (_polygonCountLimit != value)
            {
                _polygonCountLimit = value;
                Properties.Settings.Default.PolygonCountLimit = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(PolygonCountLimit));
                
                // re-validate all drawables
                foreach (var addon in MainWindow.AddonManager.Addons)
                {
                    foreach (var drawable in addon.Drawables)
                    {
                        drawable.Details.Validate();
                    }
                }
            }
        }
    }

    private SettingsHelper()
    {
        DisplaySelectedDrawablePath = Properties.Settings.Default.DisplaySelectedDrawablePath;
        PolygonCountLimit = Properties.Settings.Default.PolygonCountLimit;
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}