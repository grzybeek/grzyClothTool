using System;
using System.ComponentModel;

namespace grzyClothTool.Helpers;

public class SettingsHelper : INotifyPropertyChanged
{
    private static readonly Lazy<SettingsHelper> _instance = new(() => new SettingsHelper());
    public static SettingsHelper Instance => _instance.Value;

    public event PropertyChangedEventHandler PropertyChanged;

    private bool _displaySelectedDrawablePath;
    public bool DisplaySelectedDrawablePath
    {
        get => _displaySelectedDrawablePath;
        set => SetProperty(ref _displaySelectedDrawablePath, value, nameof(DisplaySelectedDrawablePath));
    }

    private int _polygonLimitHigh;
    public int PolygonLimitHigh
    {
        get => _polygonLimitHigh;
        set => SetProperty(ref _polygonLimitHigh, value, nameof(PolygonLimitHigh), revalidateDrawables: true);
    }

    private int _polygonLimitMed;
    public int PolygonLimitMed
    {
        get => _polygonLimitMed;
        set => SetProperty(ref _polygonLimitMed, value, nameof(PolygonLimitMed), revalidateDrawables: true);
    }

    private int _polygonLimitLow;
    public int PolygonLimitLow
    {
        get => _polygonLimitLow;
        set => SetProperty(ref _polygonLimitLow, value, nameof(PolygonLimitLow), revalidateDrawables: true);
    }

    private bool _autoDeleteFiles;
    public bool AutoDeleteFiles
    {
        get => _autoDeleteFiles;
        set => SetProperty(ref _autoDeleteFiles, value, nameof(AutoDeleteFiles));
    }

    private SettingsHelper()
    {
        _displaySelectedDrawablePath = Properties.Settings.Default.DisplaySelectedDrawablePath;
        _polygonLimitHigh = Properties.Settings.Default.PolygonLimitHigh;
        _polygonLimitMed = Properties.Settings.Default.PolygonLimitMed;
        _polygonLimitLow = Properties.Settings.Default.PolygonLimitLow;
        _autoDeleteFiles = Properties.Settings.Default.AutoDeleteFiles;
        _markNewDrawables = Properties.Settings.Default.MarkNewDrawables;
    }

    private void SetProperty<T>(ref T field, T value, string propertyName, bool revalidateDrawables = false)
    {
        if (!Equals(field, value))
        {
            field = value;
            Properties.Settings.Default[propertyName] = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(propertyName);

            if (revalidateDrawables)
            {
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

    private bool _markNewDrawables;
    public bool AutoDeleteFiles
    {
        get => _markNewDrawables;
        set => SetProperty(ref _markNewDrawables, value, nameof(MarkNewDrawables));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}