using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

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

    private bool _displayHashDuplicate;
    public bool DisplayHashDuplicate
    {
        get => _displayHashDuplicate;
        set
        {
            if (_displayHashDuplicate != value)
            {
                _displayHashDuplicate = value;
                Properties.Settings.Default.DisplayHashDuplicate = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(_displayHashDuplicate));
                MainWindow.AddonManager.ResetDuplicateSearch();
                if (value == true)
                {
                    _ = FindDuplicates();
                }
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
        DisplayHashDuplicate = Properties.Settings.Default.DisplayHashDuplicate;
        PolygonLimitHigh = Properties.Settings.Default.PolygonLimitHigh;
        PolygonLimitMed = Properties.Settings.Default.PolygonLimitMed;
        PolygonLimitLow = Properties.Settings.Default.PolygonLimitLow;
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

    private async Task FindDuplicates()
    {
        List<Task> promises = [];
        foreach (var addon in MainWindow.AddonManager.Addons)
        {
            foreach (var drawable in addon.Drawables)
            {
                promises.Add(Task.Run(async () =>
                {
                    await drawable.DrawableDetailsTask;
                    await drawable.CheckForDuplicate();
                }));
            }
        }
        await Task.WhenAll(promises);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}