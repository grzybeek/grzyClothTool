using System.Collections.ObjectModel;
using grzyClothTool.Controls;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Models;

public class GDrawableTests
{
    [Fact]
    public void Constructor_AssignsIdAndBuildsComponentName()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 11, number: 7, hasSkin: false);

        Assert.NotEqual(Guid.Empty, drawable.Id);
        Assert.Equal("jbib_007_u", drawable.Name);
        Assert.Equal("007", drawable.DisplayNumber);
        Assert.Equal("jbib", drawable.TypeName);
        Assert.True(drawable.IsComponent);
    }

    [Fact]
    public void Constructor_BuildsPropNameWithoutComponentSkinSuffix()
    {
        var drawable = CreateDrawable(SexType.female, isProp: true, typeNumeric: 0, number: 3, hasSkin: true);

        Assert.Equal("p_head_003", drawable.Name);
        Assert.Equal("003", drawable.DisplayNumber);
        Assert.Equal("p_head", drawable.TypeName);
        Assert.False(drawable.IsComponent);
    }

    [Fact]
    public void SetDrawableName_UpdatesTextureNumbersAndType()
    {
        var texture = CreateTexture(typeNumeric: 11, number: 0);
        var drawable = new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            SexType.female,
            isProp: false,
            typeNumeric: 11,
            number: 0,
            hasSkin: false,
            new ObservableCollection<GTexture> { texture });

        drawable.Number = 14;
        drawable.TypeNumeric = 4;
        drawable.TypeName = "lowr";
        drawable.SetDrawableName();

        Assert.Equal("lowr_014_u", drawable.Name);
        Assert.Equal(14, texture.Number);
        Assert.Equal(4, texture.TypeNumeric);
    }

    [Fact]
    public void HasSkin_UpdatesNameAndChildTextures()
    {
        var texture = CreateTexture(typeNumeric: 11, number: 0);
        var drawable = new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            SexType.female,
            isProp: false,
            typeNumeric: 11,
            number: 0,
            hasSkin: false,
            new ObservableCollection<GTexture> { texture });

        drawable.HasSkin = true;

        Assert.Equal("jbib_000_r", drawable.Name);
        Assert.True(texture.HasSkin);
    }

    [Fact]
    public void DisplayName_TracksWhetherCustomDisplayNameExists()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 11, number: 0, hasSkin: false);

        Assert.False(drawable.HasDisplayName);

        drawable.DisplayName = "Formal jacket";

        Assert.True(drawable.HasDisplayName);
    }

    [Fact]
    public void FlagsAndFlagsText_ReflectSelectedFlags()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 11, number: 0, hasSkin: false);

        drawable.SelectedFlags = new ObservableCollection<SelectableItem>
        {
            new("BULKY", (int)DrawableFlags.BULKY, true),
            new("COLD", (int)DrawableFlags.COLD, true),
            new("NONE", (int)DrawableFlags.NONE, true)
        };

        Assert.Equal((int)(DrawableFlags.BULKY | DrawableFlags.COLD), drawable.Flags);
        Assert.Equal($"{drawable.Flags} (2 selected)", drawable.FlagsText);
    }

    [Fact]
    public void HasTexturesNeedingOptimization_RespectsIgnoredAndOptimizedTextures()
    {
        var texture = CreateTexture(typeNumeric: 11, number: 0);
        texture.TxtDetails = new GTextureDetails { IsOptimizeNeeded = true };
        var drawable = new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            SexType.female,
            isProp: false,
            typeNumeric: 11,
            number: 0,
            hasSkin: false,
            new ObservableCollection<GTexture> { texture });

        Assert.True(drawable.HasTexturesNeedingOptimization);

        texture.IsOptimizedDuringBuild = true;

        Assert.False(drawable.HasTexturesNeedingOptimization);

        texture.IsOptimizedDuringBuild = false;
        drawable.IgnoreWarnings = true;

        Assert.False(drawable.HasTexturesNeedingOptimization);
    }

    [Fact]
    public void VisibleTagsAndHiddenTagCount_ReflectTagCollection()
    {
        var drawable = CreateDrawable(SexType.male, isProp: false, typeNumeric: 11, number: 0, hasSkin: false);

        drawable.Tags = new ObservableCollection<string> { "a", "b", "c", "d", "e" };

        Assert.True(drawable.HasTags);
        Assert.Equal(["a", "b", "c", "d"], drawable.VisibleTags);
        Assert.Equal(1, drawable.HiddenTagsCount);
    }

    private static GDrawable CreateDrawable(SexType sex, bool isProp, int typeNumeric, int number, bool hasSkin)
    {
        return new GDrawable(
            Guid.Empty,
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            sex,
            isProp,
            typeNumeric,
            number,
            hasSkin,
            new ObservableCollection<GTexture>());
    }

    private static GTexture CreateTexture(int typeNumeric, int number)
    {
        return new GTexture(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd"),
            typeNumeric,
            number,
            txtNumber: 0,
            hasSkin: false,
            isProp: false);
    }
}
