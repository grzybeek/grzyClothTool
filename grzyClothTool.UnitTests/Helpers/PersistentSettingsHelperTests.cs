using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class PersistentSettingsHelperTests
{
    [Fact]
    public void IsRootDrive_ReturnsFalseForNestedDirectory()
    {
        using var temp = new TestTempDirectory();
        var nestedPath = temp.FilePath("nested");
        Directory.CreateDirectory(nestedPath);

        Assert.False(PersistentSettingsHelper.IsRootDrive(nestedPath));
    }

    [Fact]
    public void IsRootDrive_ReturnsTrueForInvalidPath()
    {
        Assert.True(PersistentSettingsHelper.IsRootDrive("\0"));
    }

    [Fact]
    public void IsRootDrive_ReturnsTrueForDriveRoot()
    {
        var root = Path.GetPathRoot(Path.GetTempPath());

        Assert.False(string.IsNullOrEmpty(root));
        Assert.True(PersistentSettingsHelper.IsRootDrive(root!));
    }
}
