using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace grzyClothTool.Helpers;

public class GroupManager : INotifyPropertyChanged
{
    private static readonly Lazy<GroupManager> _instance = new(() => new GroupManager());
    public static GroupManager Instance => _instance.Value;

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<string> Groups => MainWindow.AddonManager?.Groups ?? [];

    private GroupManager()
    {
    }

    public void AddGroup(string groupPath)
    {
        if (string.IsNullOrWhiteSpace(groupPath))
            return;

        groupPath = groupPath.Trim();
        
        var groups = MainWindow.AddonManager?.Groups;
        if (groups != null && !groups.Contains(groupPath))
        {
            groups.Add(groupPath);
            OnPropertyChanged(nameof(Groups));
        }
    }

    public void RemoveGroup(string groupPath)
    {
        var groups = MainWindow.AddonManager?.Groups;
        if (groups != null)
        {
            groups.Remove(groupPath);
            OnPropertyChanged(nameof(Groups));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
