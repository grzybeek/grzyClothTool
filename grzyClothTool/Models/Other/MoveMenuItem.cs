using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace grzyClothTool.Models.Other;

public class MoveMenuItem : INotifyPropertyChanged
{
    private bool _isEnabled;
    public string Header { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}