using grzyClothTool.Models;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
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

        if (shouldReassignNumbers)
        {
            // Running per-(type, prop, sex) counters instead of Take(i).Count(...) per item (O(n²))
            var counters = new Dictionary<(int TypeNumeric, bool IsProp, Enums.SexType Sex), int>();
            foreach (var drawable in sorted)
            {
                var key = (drawable.TypeNumeric, drawable.IsProp, drawable.Sex);
                counters.TryGetValue(key, out var count);
                drawable.Number = count;
                drawable.SetDrawableName();
                counters[key] = count + 1;
            }
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            var currentIndex = drawables.IndexOf(sorted[i]);
            // Skip no-op moves - every Move fires CollectionChanged through grouped
            // collection views, which is the expensive part with large lists
            if (currentIndex != i)
            {
                drawables.Move(currentIndex, i);
            }
        }
    }

    public static void ReassignNumbers(this ObservableCollection<GDrawable> drawables, GDrawable drawable)
    {
        int counter = 0;

        foreach (var item in drawables.Where(x => x.IsProp == drawable.IsProp && x.Sex == drawable.Sex && x.TypeNumeric == drawable.TypeNumeric))
        {
            item.Number = counter++;
            item.SetDrawableName();
        }
    }

    public static void Sort(this ObservableCollection<Addon> addons, bool shouldReassignNumbers = false)
    {
        foreach (var addon in addons)
        {
            addon.Drawables.Sort(shouldReassignNumbers);
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
