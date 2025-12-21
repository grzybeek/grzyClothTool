using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using grzyClothTool.Helpers;

namespace grzyClothTool.Controls;

public partial class GroupEditor : UserControl
{
    public static readonly DependencyProperty GroupProperty = 
        DependencyProperty.Register(
            nameof(Group), 
            typeof(string), 
            typeof(GroupEditor), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnGroupPropertyChanged));

    public string Group
    {
        get => (string)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    private bool _isUpdatingTextBox = false;

    private static void OnGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GroupEditor editor)
        {
            editor._isUpdatingTextBox = true;
            if (editor.GroupInputBox.Text != e.NewValue?.ToString())
            {
                editor.GroupInputBox.Text = e.NewValue?.ToString() ?? string.Empty;
            }
            editor._isUpdatingTextBox = false;
        }
    }

    public GroupEditor()
    {
        InitializeComponent();
    }

    private void GroupInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingTextBox)
            return;

        var input = GroupInputBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        var allGroups = GroupManager.Instance.Groups;
        
        var suggestions = allGroups
            .Where(group => group.StartsWith(input, System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (suggestions.Count != 0)
        {
            SuggestionsList.ItemsSource = suggestions;
            SuggestionsPopup.IsOpen = true;
            
            var exactMatch = suggestions.FirstOrDefault(s => 
                s.Equals(input, System.StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
            {
                SuggestionsList.SelectedItem = exactMatch;
            }
            else
            {
                SuggestionsList.SelectedIndex = -1;
            }
        }
        else
        {
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void GroupInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (SuggestionsPopup.IsOpen)
            {
                if (SuggestionsList.SelectedItem != null)
                {
                    SelectGroup(SuggestionsList.SelectedItem.ToString());
                }
                else if (SuggestionsList.Items.Count > 0)
                {
                    SelectGroup(SuggestionsList.Items[0].ToString());
                }
                else
                {
                    SaveGroup(GroupInputBox.Text);
                }
            }
            else
            {
                SaveGroup(GroupInputBox.Text);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down && SuggestionsPopup.IsOpen)
        {
            if (SuggestionsList.SelectedIndex < 0 && SuggestionsList.Items.Count > 0)
            {
                SuggestionsList.SelectedIndex = 0;
            }
            SuggestionsList.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    private void GroupInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            if (!SuggestionsList.IsKeyboardFocusWithin && !GroupInputBox.IsKeyboardFocused)
            {
                SaveGroup(GroupInputBox.Text);
                SuggestionsPopup.IsOpen = false;
            }
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SuggestionsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is string selectedGroup)
        {
            SelectGroup(selectedGroup);
            e.Handled = true;
        }
    }

    private void SuggestionsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SuggestionsList.SelectedItem != null)
        {
            SelectGroup(SuggestionsList.SelectedItem.ToString());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            GroupInputBox.Focus();
            e.Handled = true;
        }
    }

    private void SelectGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return;

        groupName = groupName.Trim();
        
        _isUpdatingTextBox = true;
        Group = groupName;
        GroupInputBox.Text = groupName;
        _isUpdatingTextBox = false;
        
        GroupManager.Instance.AddGroup(groupName);

        SuggestionsPopup.IsOpen = false;
    }

    private void SaveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            _isUpdatingTextBox = true;
            Group = null;
            _isUpdatingTextBox = false;
            return;
        }

        groupName = groupName.Trim();
        
        _isUpdatingTextBox = true;
        Group = groupName;
        _isUpdatingTextBox = false;
        
        GroupManager.Instance.AddGroup(groupName);
    }
}

