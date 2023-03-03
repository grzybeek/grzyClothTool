using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace gClothTool
{
    public class AddonManager
    {
        public ObservableCollection<Component> Components = new ObservableCollection<Component>();


        public AddonManager()
        {
            GenerateComponents();
        }

        private void GenerateComponents()
        {
            foreach(int id in Enum.GetValues(typeof(Enums.ComponentNumbers)))
            {
                string comp = Enum.GetName(typeof(Enums.ComponentNumbers), id);

                Components.Add(new Component(id, comp));
            }
        }


        public Component GetComponent(string input)
        {
            Trace.WriteLine(input);

            string[] values = input.Split("^");
            string name = values[^1].Split("_")[0].ToLower();
            int enumNumber;

            if (Enum.IsDefined(typeof(Enums.ComponentNumbers), name))
            {
                enumNumber = (int)(Enums.ComponentNumbers)Enum.Parse(typeof(Enums.ComponentNumbers), name.ToLower());
            }
            else
            {
                //todo: ask for type
                throw new NotImplementedException($"{name} nie jest poprawnym typem");
            }

            

            return Components.ElementAt(enumNumber);
        }


    }
}
