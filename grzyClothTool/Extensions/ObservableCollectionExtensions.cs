using grzyClothTool.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace grzyClothTool.Extensions;

public static class ObservableCollectionExtensions
{
    public static void Sort(this ObservableCollection<Drawable> drawables)
    {
        var sorted = drawables.OrderBy(x => x.Sex).ThenBy(x => x.Name).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            drawables.Move(drawables.IndexOf(sorted[i]), i);
        }
    }
}
