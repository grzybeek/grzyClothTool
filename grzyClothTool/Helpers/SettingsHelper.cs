using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

    private SettingsHelper()
    {
        DisplaySelectedDrawablePath = Properties.Settings.Default.DisplaySelectedDrawablePath;
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}