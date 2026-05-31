using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class ErrorLogHelperTests
{
    [Fact]
    public void GetLogFileLocation_ReturnsErrorLogInApplicationBaseDirectory()
    {
        var location = ErrorLogHelper.GetLogFileLocation();

        Assert.Equal(AppDomain.CurrentDomain.BaseDirectory, Path.GetDirectoryName(location) + Path.DirectorySeparatorChar);
        Assert.Equal("grzyClothTool_errors.log", Path.GetFileName(location));
    }

    [Fact]
    public void LogError_WritesMessageAndExceptionDetails()
    {
        var location = ErrorLogHelper.GetLogFileLocation();
        var marker = $"unit-test-{Guid.NewGuid():N}";
        var exception = new InvalidOperationException("outer problem", new ArgumentException("inner problem"));

        ErrorLogHelper.LogError(marker, exception);

        var log = File.ReadAllText(location);
        Assert.Contains(marker, log);
        Assert.Contains("InvalidOperationException: outer problem", log);
        Assert.Contains("Inner Exception: ArgumentException: inner problem", log);
    }
}
