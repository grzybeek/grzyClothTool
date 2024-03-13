using grzyClothTool.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for DrawableList.xaml
    /// </summary>
    public partial class DrawableList : UserControl
    {
        public event EventHandler DrawableListSelectedValueChanged;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(ObservableCollection<GDrawable>), typeof(DrawableList), new PropertyMetadata(default(ObservableCollection<GDrawable>)));

        public ObservableCollection<GDrawable> ItemsSource
        {
            get { return (ObservableCollection<GDrawable>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }


        public object DrawableListSelectedValue
        {
            get { return MyListBox.SelectedValue; }
        }

        public DrawableList()
        {
            InitializeComponent();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             DrawableListSelectedValueChanged?.Invoke(sender, e);
        }
    }
}
