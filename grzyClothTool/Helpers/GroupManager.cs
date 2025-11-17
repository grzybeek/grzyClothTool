using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace grzyClothTool.Helpers;

public class GroupManager : INotifyPropertyChanged
{
    private static readonly Lazy<GroupManager> _instance = new(() => new GroupManager());
    public static GroupManager Instance => _instance.Value;

    public event PropertyChangedEventHandler PropertyChanged;

    private ObservableCollection<string> _groups;
    public ObservableCollection<string> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            OnPropertyChanged(nameof(Groups));
        }
    }

    private GroupManager()
    {
        Groups = [];
    }

    public void AddGroup(string groupPath)
    {
        if (string.IsNullOrWhiteSpace(groupPath))
            return;

        groupPath = groupPath.Trim();
        
        if (!Groups.Contains(groupPath))
        {
            Groups.Add(groupPath);
            OnPropertyChanged(nameof(Groups));
        }
    }

    public void RemoveGroup(string groupPath)
    {
        Groups.Remove(groupPath);
        OnPropertyChanged(nameof(Groups));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
