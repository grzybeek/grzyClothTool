using grzyClothTool.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace grzyClothTool.Helpers;
public class NavigationHelper : INotifyPropertyChanged
{
    private readonly Dictionary<string, Func<UserControl>> _pageFactories = [];
    private readonly Dictionary<string, UserControl> _pages = [];

    public event PropertyChangedEventHandler PropertyChanged;

    private UserControl _currentPage;
    public UserControl CurrentPage
    {
        get { return _currentPage; }
        set
        {
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    public NavigationHelper()
    {
        CurrentPage = new ProjectWindow();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void RegisterPage(string pageKey, Func<UserControl> pageFactory)
    {
        _pageFactories.TryAdd(pageKey, pageFactory);
    }

    public void Navigate(string pageKey)
    {
        if (_pageFactories.TryGetValue(pageKey, out var pageFactory))
        {
            if (!_pages.TryGetValue(pageKey, out UserControl page))
            {
                page = pageFactory.Invoke();
                _pages.Add(pageKey, page);
            }

            MainWindow.Instance.MainWindowContentControl.Content = page;
        }
    }
}
