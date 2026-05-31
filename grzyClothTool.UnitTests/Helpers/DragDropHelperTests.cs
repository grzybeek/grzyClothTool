using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class DragDropHelperTests
{
    [Fact]
    public void CreateExtensionFilter_MatchesExtensionsCaseInsensitively()
    {
        var filter = DragDropHelper.CreateExtensionFilter(".ydd", ".ytd");

        Assert.True(filter("asset.YDD"));
        Assert.True(filter(@"C:\tmp\texture.ytd"));
        Assert.False(filter("image.png"));
    }

    [Fact]
    public void CreateExtensionFilter_ReturnsFalseWhenFileHasNoExtension()
    {
        var filter = DragDropHelper.CreateExtensionFilter(".ydd");

        Assert.False(filter("drawable"));
    }

    [Fact]
    public void ValidateFileAccess_SplitsExistingAndMissingFiles()
    {
        using var temp = new TestTempDirectory();
        var existing = temp.FilePath("existing.ydd");
        var missing = temp.FilePath("missing.ydd");
        File.WriteAllBytes(existing, []);

        var (accessible, inaccessible) = DragDropHelper.ValidateFileAccess([existing, missing]);

        Assert.Equal([existing], accessible);
        Assert.Equal([missing], inaccessible);
    }

    [Fact]
    public void ValidateFileAccess_ReturnsEmptyListsForEmptyInput()
    {
        var (accessible, inaccessible) = DragDropHelper.ValidateFileAccess([]);

        Assert.Empty(accessible);
        Assert.Empty(inaccessible);
    }
}
