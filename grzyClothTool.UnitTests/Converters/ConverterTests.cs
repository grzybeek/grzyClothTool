using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using grzyClothTool.Converters;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Converters;

public class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    [InlineData(null, Visibility.Collapsed)]
    public void BooleanToVisibilityConverter_ConvertsBooleanToVisibility(bool? value, Visibility expected)
    {
        var converter = new BooleanToVisibilityConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(Visibility), null, Culture));
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Collapsed, false)]
    [InlineData(null, false)]
    public void BooleanToVisibilityConverter_ConvertsBackVisibilityToBoolean(Visibility? value, bool expected)
    {
        var converter = new BooleanToVisibilityConverter();

        Assert.Equal(expected, converter.ConvertBack(value, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    [InlineData(null, Visibility.Visible)]
    public void InverseBooleanToVisibilityConverter_InvertsBooleanVisibility(bool? value, Visibility expected)
    {
        var converter = new InverseBooleanToVisibilityConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(Visibility), null, Culture));
    }

    [Theory]
    [InlineData(Visibility.Visible, false)]
    [InlineData(Visibility.Collapsed, true)]
    [InlineData(Visibility.Hidden, true)]
    public void InverseBooleanToVisibilityConverter_ConvertsBackVisibilityToInvertedBoolean(
        Visibility value,
        bool expected)
    {
        var converter = new InverseBooleanToVisibilityConverter();

        Assert.Equal(expected, converter.ConvertBack(value, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(1, Visibility.Visible)]
    [InlineData(0, Visibility.Collapsed)]
    [InlineData(-1, Visibility.Collapsed)]
    [InlineData(null, Visibility.Collapsed)]
    public void IntToVisibilityConverter_OnlyPositiveIntegersAreVisible(int? value, Visibility expected)
    {
        var converter = new IntToVisibilityConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(Visibility), null, Culture));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InverseBooleanConverter_InvertsBooleanValues(bool value, bool expected)
    {
        var converter = new InverseBooleanConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(bool), null, Culture));
        Assert.Equal(expected, converter.ConvertBack(value, typeof(bool), null, Culture));
    }

    [Fact]
    public void InverseBooleanConverter_ReturnsNonBooleanValuesUnchanged()
    {
        var converter = new InverseBooleanConverter();

        Assert.Equal("yes", converter.Convert("yes", typeof(bool), null, Culture));
    }

    [Fact]
    public void UpperCaseConverter_UpperCasesStringsAndLeavesOtherValuesAlone()
    {
        var converter = new UpperCaseConverter();

        Assert.Equal("JBIB", converter.Convert("jbib", typeof(string), null, Culture));
        Assert.Equal(7, converter.Convert(7, typeof(string), null, Culture));
        Assert.Equal("jbib", converter.ConvertBack("jbib", typeof(string), null, Culture));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("abc", true)]
    public void NullOrEmptyToFalseConverter_ReturnsTrueOnlyForNonEmptyStrings(string? value, bool expected)
    {
        var converter = new NullOrEmptyToFalseConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(@"C:\projects\asset\jbib_000_u.ydd", "asset/jbib_000_u.ydd")]
    [InlineData("", "")]
    public void FilePathToShortVersionConverter_ShowsParentFolderAndFileName(string value, object expected)
    {
        var converter = new FilePathToShortVersionConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(string), null, Culture));
    }

    [Theory]
    [InlineData("#FF112233")]
    [InlineData("#112233")]
    public void HexColorToBrushConverter_ConvertsValidColorStrings(string value)
    {
        var converter = new HexColorToBrushConverter();

        var result = Assert.IsType<SolidColorBrush>(converter.Convert(value, typeof(SolidColorBrush), null, Culture));

        Assert.NotEqual(Colors.Transparent, result.Color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-color")]
    public void HexColorToBrushConverter_ReturnsTransparentForInvalidValues(string? value)
    {
        var converter = new HexColorToBrushConverter();

        Assert.Same(Brushes.Transparent, converter.Convert(value, typeof(SolidColorBrush), null, Culture));
    }

    [Fact]
    public void HexColorToBrushConverter_ConvertsBrushBackToColorString()
    {
        var converter = new HexColorToBrushConverter();
        var brush = new SolidColorBrush(Color.FromArgb(255, 17, 34, 51));

        Assert.Equal("#FF112233", converter.ConvertBack(brush, typeof(string), null, Culture));
        Assert.Null(converter.ConvertBack("not-a-brush", typeof(string), null, Culture));
    }

    [Theory]
    [InlineData("D3DFMT_DXT1", "DXT1")]
    [InlineData("DXT5", "DXT5")]
    [InlineData("BC7", "BC7")]
    [InlineData("UNKNOWN", "UNKNOWN")]
    [InlineData("unexpected", "OTHER")]
    [InlineData(null, "N/A")]
    public void CompressionFormatConverter_NormalizesKnownFormats(string? value, string expected)
    {
        var converter = new CompressionFormatConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(string), null, Culture));
    }

    [Theory]
    [InlineData(BuildResourceType.FiveM, "FiveM", true)]
    [InlineData(BuildResourceType.AltV, "FiveM", false)]
    public void EnumToBoolConverter_ConvertsEnumToCheckedState(BuildResourceType value, string parameter, bool expected)
    {
        var converter = new EnumToBoolConverter();

        Assert.Equal(expected, converter.Convert(value, typeof(bool), parameter, Culture));
    }

    [Fact]
    public void EnumToBoolConverter_ConvertsCheckedStateBackToEnum()
    {
        var converter = new EnumToBoolConverter();

        Assert.Equal(BuildResourceType.FiveM, converter.ConvertBack(true, typeof(BuildResourceType), "FiveM", Culture));
        Assert.Same(Binding.DoNothing, converter.ConvertBack(false, typeof(BuildResourceType), "FiveM", Culture));
    }
}
