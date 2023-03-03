using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace gClothTool
{
    public class Component
    {

        public int compId;
        public string compType { get; set; }
        public int compDrawablesCount
        {
            get
            {
                return compDrawables.Count;
            }
        }

        public string compTypeHeader
        {
            get
            {
                return compType + $" ({compDrawablesCount})";
            }
        }

        public ObservableCollection<Drawable> compDrawables { get; set; }

        public Component(int id, string type)
        {
            compId = id;
            compType = type;
            compDrawables = new ObservableCollection<Drawable>();
        }

        public void AddDrawableToComponent(Drawable drawable)
        {
            compDrawables.Add(drawable);
        }
    }
}
