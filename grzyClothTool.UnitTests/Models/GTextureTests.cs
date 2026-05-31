using grzyClothTool.Models.Texture;

namespace grzyClothTool.UnitTests.Models;

public class GTextureTests
{
    [Fact]
    public void Constructor_AssignsIdWhenEmptyGuidProvided()
    {
        var texture = CreateTexture(Guid.Empty, typeNumeric: 11, number: 0, txtNumber: 0, hasSkin: false, isProp: false);

        Assert.NotEqual(Guid.Empty, texture.Id);
    }

    [Theory]
    [InlineData(11, 5, 0, false, false, "jbib_diff_005_a_uni")]
    [InlineData(11, 5, 1, true, false, "jbib_diff_005_b_whi")]
    [InlineData(0, 3, 2, false, true, "p_head_diff_003_c")]
    public void GetBuildName_UsesTypeNumberTextureLetterSkinAndPropState(
        int typeNumeric,
        int number,
        int txtNumber,
        bool hasSkin,
        bool isProp,
        string expected)
    {
        var texture = CreateTexture(Guid.NewGuid(), typeNumeric, number, txtNumber, hasSkin, isProp);

        Assert.Equal(expected, texture.GetBuildName());
        Assert.Equal(expected, texture.DisplayName);
    }

    [Fact]
    public void NumberAndTextureNumber_UpdateDisplayName()
    {
        var texture = CreateTexture(Guid.NewGuid(), typeNumeric: 11, number: 0, txtNumber: 0, hasSkin: false, isProp: false);

        texture.Number = 12;
        texture.TxtNumber = 2;

        Assert.Equal('c', texture.TxtLetter);
        Assert.Equal("jbib_diff_012_c_uni", texture.DisplayName);
    }

    private static GTexture CreateTexture(Guid id, int typeNumeric, int number, int txtNumber, bool hasSkin, bool isProp)
    {
        return new GTexture(
            id,
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd"),
            typeNumeric,
            number,
            txtNumber,
            hasSkin,
            isProp);
    }
}
