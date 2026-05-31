using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class ImgHelperTests
{
    [Theory]
    [InlineData(2048, 2048, 10)]
    [InlineData(1024, 512, 8)]
    [InlineData(128, 256, 6)]
    public void GetCorrectMipMapAmount_UsesSmallestDimension(int width, int height, int expected)
    {
        Assert.Equal(expected, ImgHelper.GetCorrectMipMapAmount(width, height));
    }

    [Theory]
    [InlineData(512, 512, 512, 512)]
    [InlineData(513, 512, 1024, 512)]
    [InlineData(300, 700, 512, 1024)]
    public void CheckPowerOfTwo_ReturnsNextPowerOfTwoWhenNeeded(
        int width,
        int height,
        int expectedWidth,
        int expectedHeight)
    {
        Assert.Equal((expectedWidth, expectedHeight), ImgHelper.CheckPowerOfTwo(width, height));
    }
}
