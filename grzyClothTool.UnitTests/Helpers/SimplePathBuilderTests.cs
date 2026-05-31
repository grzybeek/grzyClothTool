using System.Collections.ObjectModel;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class SimplePathBuilderTests
{
    [Fact]
    public void BuildPath_AddsGenderGroupAndTypeForFiveMGroupedDrawable()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 11);
        drawable.Group = "tops/jackets";
        var buildPath = Path.Combine("C:", "build");

        var path = SimplePathBuilder.BuildPath(drawable, buildPath, BuildResourceType.FiveM);

        Assert.Equal(Path.Combine(buildPath, "stream", "[male]", "tops", "jackets", "jbib"), path);
    }

    [Fact]
    public void BuildPath_OmitsGroupWhenResourceTypeDoesNotUseGroups()
    {
        var drawable = CreateDrawable(SexType.female, isProp: true, typeNumeric: 0);
        drawable.Group = "hats";
        var buildPath = Path.Combine("C:", "build");

        var path = SimplePathBuilder.BuildPath(drawable, buildPath, BuildResourceType.AltV);

        Assert.Equal(Path.Combine(buildPath, "stream", "[female]", "p_head"), path);
    }

    [Fact]
    public void BuildPath_NormalizesBackslashGroupSeparatorsForFiveM()
    {
        var drawable = CreateDrawable(SexType.female, isProp: false, typeNumeric: 4);
        drawable.Group = @"pants\formal";
        var buildPath = Path.Combine("C:", "build");

        var path = SimplePathBuilder.BuildPath(drawable, buildPath, BuildResourceType.FiveM);

        Assert.Equal(Path.Combine(buildPath, "stream", "[female]", "pants", "formal", "lowr"), path);
    }

    [Fact]
    public void BuildPath_OmitsEmptyGroupForFiveM()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 6);
        drawable.Group = " ";
        var buildPath = Path.Combine("C:", "build");

        var path = SimplePathBuilder.BuildPath(drawable, buildPath, BuildResourceType.FiveM);

        Assert.Equal(Path.Combine(buildPath, "stream", "[male]", "feet"), path);
    }

    private static GDrawable CreateDrawable(SexType sex, bool isProp, int typeNumeric)
    {
        return new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            sex,
            isProp,
            typeNumeric,
            0,
            hasSkin: false,
            new ObservableCollection<GTexture>());
    }
}
