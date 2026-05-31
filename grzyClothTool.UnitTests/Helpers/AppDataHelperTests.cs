using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class AppDataHelperTests
{
    private const string OverrideVariable = "GRZYCLOTHTOOL_LOCALAPPDATA";

    [Fact]
    public void GetLocalAppDataPath_UsesEnvironmentOverrideWhenPresent()
    {
        var original = Environment.GetEnvironmentVariable(OverrideVariable);
        using var temp = new TestTempDirectory();

        try
        {
            Environment.SetEnvironmentVariable(OverrideVariable, temp.Path);

            Assert.Equal(temp.Path, AppDataHelper.GetLocalAppDataPath());
        }
        finally
        {
            Environment.SetEnvironmentVariable(OverrideVariable, original);
        }
    }

    [Fact]
    public void GetLocalAppDataPath_FallsBackToSpecialFolderWhenOverrideIsBlank()
    {
        var original = Environment.GetEnvironmentVariable(OverrideVariable);

        try
        {
            Environment.SetEnvironmentVariable(OverrideVariable, " ");

            Assert.Equal(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppDataHelper.GetLocalAppDataPath());
        }
        finally
        {
            Environment.SetEnvironmentVariable(OverrideVariable, original);
        }
    }
}
