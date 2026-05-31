using grzyClothTool.Constants;
using grzyClothTool.Helpers;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class PedAlternativeVariationsHelperTests
{
    [Theory]
    [InlineData(SexType.male)]
    [InlineData(SexType.female)]
    public void GetHairEntriesForSex_ReturnsConfiguredListForSex(SexType sex)
    {
        var entries = PedAlternativeVariationsHelper.GetHairEntriesForSex(sex);

        Assert.Same(
            sex == SexType.male
                ? PedAlternateVariationsConstants.MaleHairs
                : PedAlternateVariationsConstants.FemaleHairs,
            entries);
        Assert.NotEmpty(entries);
    }

    [Theory]
    [InlineData(SexType.male)]
    [InlineData(SexType.female)]
    public void GetMaskEntriesForSex_ReturnsConfiguredListForSex(SexType sex)
    {
        var entries = PedAlternativeVariationsHelper.GetMaskEntriesForSex(sex);

        Assert.Same(
            sex == SexType.male
                ? PedAlternateVariationsConstants.MaleMasks
                : PedAlternateVariationsConstants.FemaleMasks,
            entries);
        Assert.NotEmpty(entries);
    }
}
