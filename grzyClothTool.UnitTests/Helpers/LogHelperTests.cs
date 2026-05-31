using grzyClothTool.Helpers;
using grzyClothTool.Views;

namespace grzyClothTool.UnitTests.Helpers;

public class LogHelperTests
{
    [Theory]
    [InlineData(LogType.Info, "Check")]
    [InlineData(LogType.Warning, "WarningOutline")]
    [InlineData(LogType.Error, "Close")]
    public void GetLogTypeIcon_ReturnsConfiguredIconForKnownLogTypes(LogType logType, string expected)
    {
        Assert.Equal(expected, LogHelper.GetLogTypeIcon(logType));
    }

    [Fact]
    public void GetLogTypeIcon_ReturnsInfoIconForUnknownLogType()
    {
        Assert.Equal("Info", LogHelper.GetLogTypeIcon((LogType)999));
    }

    [Fact]
    public void Log_DoesNothingWhenLogWindowHasNotBeenInitialized()
    {
        LogHelper.Log("message without a log window");
    }
}
