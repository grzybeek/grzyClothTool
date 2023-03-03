using System;
using System.Collections.Generic;
using System.Text;

namespace gClothTool
{
    public class Drawable
    {

        private string Path;
        public string Name { get; set; }

        public Drawable(string path, string name)
        {
            Path = path;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
