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

    private int _polygonLimitHigh;
    public int PolygonLimitHigh
    {
        get => _polygonLimitHigh;
        set => SetPolygonLimit(ref _polygonLimitHigh, value, nameof(PolygonLimitHigh));
    }

    private int _polygonLimitMed;
    public int PolygonLimitMed
    {
        get => _polygonLimitMed;
        set => SetPolygonLimit(ref _polygonLimitMed, value, nameof(PolygonLimitMed));
    }

    private int _polygonLimitLow;
    public int PolygonLimitLow
    {
        get => _polygonLimitLow;
        set => SetPolygonLimit(ref _polygonLimitLow, value, nameof(PolygonLimitLow));
    }

    private SettingsHelper()
    {
        DisplaySelectedDrawablePath = Properties.Settings.Default.DisplaySelectedDrawablePath;
        PolygonLimitHigh = Properties.Settings.Default.PolygonLimitHigh;
        PolygonLimitMed = Properties.Settings.Default.PolygonLimitMed;
        PolygonLimitLow = Properties.Settings.Default.PolygonLimitLow;
        MarkNewDrawables = Properties.Settings.Default.MarkNewDrawables;
    }

    private void SetPolygonLimit(ref int field, int value, string propertyName)
    {
        if (field != value)
        {
            field = value;
            Properties.Settings.Default[propertyName] = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(propertyName);

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

    private bool _markNewDrawables;

    public bool MarkNewDrawables
    {
        get => _markNewDrawables;
        set
        {
            if (_markNewDrawables != value)
            {
                _markNewDrawables = value;
                Properties.Settings.Default.MarkNewDrawables = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(MarkNewDrawables));
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}