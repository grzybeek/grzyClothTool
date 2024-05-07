using grzyClothTool.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace grzyClothTool.Extensions;

public static class ObservableCollectionExtensions
{
    public static void Sort(this ObservableCollection<GDrawable> drawables, bool shouldReassignNumbers = false)
    {
        var sorted = drawables.OrderBy(x => x.Sex)
                              .ThenBy(x => x.Name)
                              .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (shouldReassignNumbers)
            {
                sorted[i].Number = sorted.Take(i).Count(x => x.TypeNumeric == sorted[i].TypeNumeric && x.IsProp == sorted[i].IsProp && x.Sex == sorted[i].Sex);
                sorted[i].SetDrawableName();
            }
            drawables.Move(drawables.IndexOf(sorted[i]), i);
        }
    }

    public static void ReassignNumbers(this ObservableCollection<GTexture> textures)
    {
        for (int i = 0; i < textures.Count; i++)
        {
            textures[i].TxtNumber = i;
        }
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
    {
        return new ObservableCollection<T>(source);
    }
}
