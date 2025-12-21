using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace grzyClothTool.Helpers;

public class TagManager : INotifyPropertyChanged
{
    private static readonly Lazy<TagManager> _instance = new(() => new TagManager());
    public static TagManager Instance => _instance.Value;

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<string> Tags => MainWindow.AddonManager?.Tags ?? [];

    private TagManager()
    {
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        tag = tag.Trim().ToUpper();
        
        var tags = MainWindow.AddonManager?.Tags;
        if (tags != null && !tags.Contains(tag))
        {
            tags.Add(tag);
            OnPropertyChanged(nameof(Tags));
        }
    }

    public void RemoveTag(string tag)
    {
        var tags = MainWindow.AddonManager?.Tags;
        if (tags != null)
        {
            tags.Remove(tag);
            OnPropertyChanged(nameof(Tags));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

