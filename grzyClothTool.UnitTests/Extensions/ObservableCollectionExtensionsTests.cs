using System.Collections.ObjectModel;
using grzyClothTool.Extensions;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Extensions;

public class ObservableCollectionExtensionsTests
{
    [Fact]
    public void Sort_ReassignsSequentialNumbersInNameOrder()
    {
        // Simulates an import of 11 drawables that were assigned numbers 0..10
        // in arrival order, but sit shuffled in the collection.
        var drawables = new ObservableCollection<GDrawable>();
        int[] shuffled = [7, 2, 10, 0, 5, 9, 1, 4, 8, 3, 6];
        foreach (var number in shuffled)
        {
            drawables.Add(CreateDrawable(SexType.male, typeNumeric: 11, number));
        }

        drawables.Sort(shouldReassignNumbers: true);

        // After sorting, numbers must be 0..10 in collection order and names must match.
        for (int i = 0; i <= 10; i++)
        {
            Assert.Equal(i, drawables[i].Number);
            Assert.Equal($"jbib_{i:D3}_u", drawables[i].Name);
        }
    }

    [Fact]
    public void Sort_NumbersEachTypeAndSexGroupIndependently()
    {
        var drawables = new ObservableCollection<GDrawable>
        {
            CreateDrawable(SexType.female, typeNumeric: 11, number: 1),
            CreateDrawable(SexType.male, typeNumeric: 4, number: 1),
            CreateDrawable(SexType.male, typeNumeric: 11, number: 1),
            CreateDrawable(SexType.female, typeNumeric: 11, number: 0),
            CreateDrawable(SexType.male, typeNumeric: 4, number: 0),
            CreateDrawable(SexType.male, typeNumeric: 11, number: 0),
        };

        drawables.Sort(shouldReassignNumbers: true);

        // Females sort first (SexType.female = 0); within each (sex, type) group numbering restarts at 0.
        Assert.All(drawables.Take(2), d => Assert.Equal(SexType.female, d.Sex));
        Assert.All(drawables.Skip(2), d => Assert.Equal(SexType.male, d.Sex));

        foreach (var group in drawables.GroupBy(d => (d.Sex, d.TypeNumeric)))
        {
            Assert.Equal(Enumerable.Range(0, group.Count()), group.Select(d => d.Number));
        }
    }

    [Fact]
    public void Sort_AlreadySortedCollectionKeepsOrderAndNumbers()
    {
        var drawables = new ObservableCollection<GDrawable>();
        for (int i = 0; i < 5; i++)
        {
            drawables.Add(CreateDrawable(SexType.male, typeNumeric: 11, number: i));
        }
        var originalOrder = drawables.ToList();

        var moveEvents = 0;
        drawables.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
            {
                moveEvents++;
            }
        };

        drawables.Sort(shouldReassignNumbers: true);

        Assert.Equal(originalOrder, drawables);
        Assert.Equal(Enumerable.Range(0, 5), drawables.Select(d => d.Number));
        Assert.Equal(0, moveEvents); // no-op moves must be skipped
    }

    private static GDrawable CreateDrawable(SexType sex, int typeNumeric, int number)
    {
        return new GDrawable(
            Guid.Empty,
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            sex,
            isProp: false,
            typeNumeric,
            number,
            hasSkin: false,
            new ObservableCollection<GTexture>());
    }
}
