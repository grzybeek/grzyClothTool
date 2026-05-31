using grzyClothTool.Helpers;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class EnumHelperTests
{
    [Theory]
    [InlineData(11, false, "jbib")]
    [InlineData(0, true, "p_head")]
    public void GetName_ReturnsComponentOrPropName(int value, bool isProp, string expected)
    {
        Assert.Equal(expected, EnumHelper.GetName(value, isProp));
    }

    [Theory]
    [InlineData("jbib", false, 11)]
    [InlineData("p_head", true, 0)]
    public void GetValue_ReturnsComponentOrPropValue(string name, bool isProp, int expected)
    {
        Assert.Equal(expected, EnumHelper.GetValue(name, isProp));
    }

    [Fact]
    public void GetFlags_MarksSelectedBitFlags()
    {
        var flags = EnumHelper.GetFlags((int)(DrawableFlags.BULKY | DrawableFlags.COLD));

        Assert.Contains(flags, flag => flag.Text == "BULKY" && flag.IsSelected);
        Assert.Contains(flags, flag => flag.Text == "COLD" && flag.IsSelected);
        Assert.DoesNotContain(flags, flag => flag.Text == "NONE");
    }

    [Theory]
    [InlineData((int)ComponentNumbers.berd, new[] { "none", "cloth_scuba" })]
    [InlineData((int)ComponentNumbers.feet, new[] { "shoe_high_heels", "shoe_silent" })]
    [InlineData((int)ComponentNumbers.hair, new[] { "none" })]
    public void GetAudioList_ReturnsExpectedOptionsForDrawableType(int typeNumeric, string[] expectedItems)
    {
        var audioList = EnumHelper.GetAudioList(typeNumeric);

        foreach (var expectedItem in expectedItems)
        {
            Assert.Contains(expectedItem, audioList);
        }
    }

    [Fact]
    public void TypeLists_ExposeKnownDrawablePropAndSexValues()
    {
        Assert.Contains("jbib", EnumHelper.GetDrawableTypeList());
        Assert.Contains("p_head", EnumHelper.GetPropTypeList());
        Assert.Contains("male", EnumHelper.GetSexTypeList());
        Assert.Contains("female", EnumHelper.GetSexTypeList());
    }
}
