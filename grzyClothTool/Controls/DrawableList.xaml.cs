using grzyClothTool.Models;
using System;
using System.Windows.Controls;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for DrawableList.xaml
    /// </summary>
    public partial class DrawableList : UserControl
    {
        public event EventHandler DrawableListSelectedValueChanged;

        public object DrawableListSelectedValue
        {
            get { return MyListBox.SelectedValue; }
        }

        public DrawableList()
        {
            InitializeComponent();
            MyListBox.SelectionChanged += ListBox_SelectionChanged;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = MyListBox.SelectedItem as Drawable;
            if (item != null)
            {
                //
            }

             DrawableListSelectedValueChanged?.Invoke(sender, e);
        }
    }
}
