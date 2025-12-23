using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using grzyClothTool.Helpers;

namespace grzyClothTool.Controls;

public partial class TagsEditor : UserControl
{
    public event EventHandler TagsChanged;

    public static readonly DependencyProperty TagsProperty = 
        DependencyProperty.Register(
            nameof(Tags), 
            typeof(ObservableCollection<string>), 
            typeof(TagsEditor), 
            new PropertyMetadata(null));

    public ObservableCollection<string> Tags
    {
        get => (ObservableCollection<string>)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public TagsEditor()
    {
        InitializeComponent();
    }

    private void TagInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var input = TagInputBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        var allTags = TagManager.Instance.Tags;
        
        var suggestions = allTags
            .Where(tag => tag.StartsWith(input, System.StringComparison.OrdinalIgnoreCase))
            .Where(tag => Tags?.Contains(tag) != true)
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

    private void TagInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (SuggestionsPopup.IsOpen)
            {
                if (SuggestionsList.SelectedItem != null)
                {
                    AddTag(SuggestionsList.SelectedItem.ToString());
                }
                else if (SuggestionsList.Items.Count > 0)
                {
                    AddTag(SuggestionsList.Items[0].ToString());
                }
                else
                {
                    AddTag(TagInputBox.Text);
                }
            }
            else
            {
                AddTag(TagInputBox.Text);
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

    private void TagInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new System.Action(() =>
        {
            if (!SuggestionsList.IsKeyboardFocusWithin && !TagInputBox.IsKeyboardFocused)
            {
                SuggestionsPopup.IsOpen = false;
            }
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void SuggestionsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is string selectedTag)
        {
            AddTag(selectedTag);
            e.Handled = true;
        }
    }

    private void SuggestionsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SuggestionsList.SelectedItem != null)
        {
            AddTag(SuggestionsList.SelectedItem.ToString());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            TagInputBox.Focus();
            e.Handled = true;
        }
    }

    private void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        AddTag(TagInputBox.Text);
    }

    private void AddTag(string tagText)
    {
        tagText = tagText?.Trim().ToUpper();
        
        if (string.IsNullOrWhiteSpace(tagText))
            return;

        if (tagText.Length > 20)
        {
            tagText = tagText.Substring(0, 20);
        }

        Tags ??= [];

        if (Tags.Any(t => t.Equals(tagText, System.StringComparison.OrdinalIgnoreCase)))
        {
            TagInputBox.Clear();
            SuggestionsPopup.IsOpen = false;
            TagInputBox.Focus();
            return;
        }

        Tags.Add(tagText);
        
        TagManager.Instance.AddTag(tagText);

        TagInputBox.Clear();
        SuggestionsPopup.IsOpen = false;
        TagInputBox.Focus();
        
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            Tags?.Remove(tag);
            TagsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}





