using System.Collections.ObjectModel;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class DuplicateDetectorTests : IDisposable
{
    public DuplicateDetectorTests()
    {
        DuplicateDetector.Clear();
    }

    public void Dispose()
    {
        DuplicateDetector.Clear();
    }

    [Fact]
    public void RegisterDrawable_GroupsDrawablesWithMatchingHash()
    {
        using var temp = new TestTempDirectory();
        var first = CreateDrawable(temp.FilePath("first.ydd"));
        var second = CreateDrawable(temp.FilePath("second.ydd"));
        WriteBytes(first.FullFilePath, [1, 2, 3, 4]);
        WriteBytes(second.FullFilePath, [5, 6, 7, 8]);

        DuplicateDetector.RegisterDrawable(first);
        DuplicateDetector.RegisterDrawable(second);

        Assert.True(first.DuplicateInfo.IsDuplicate);
        Assert.True(second.DuplicateInfo.IsDuplicate);
        Assert.Equal(2, first.DuplicateInfo.DuplicateCount);
        Assert.Equal(first.DuplicateInfo.DuplicateGroupId, second.DuplicateInfo.DuplicateGroupId);
        Assert.Equal(1, DuplicateDetector.GetDuplicateGroupCount());
    }

    [Fact]
    public void RegisterDrawable_IgnoresSameInstanceWhenRegisteredTwice()
    {
        using var temp = new TestTempDirectory();
        var drawable = CreateDrawable(temp.FilePath("single.ydd"));
        WriteBytes(drawable.FullFilePath, [1, 2, 3, 4]);

        DuplicateDetector.RegisterDrawable(drawable);
        DuplicateDetector.RegisterDrawable(drawable);

        Assert.False(drawable.DuplicateInfo.IsDuplicate);
        Assert.Equal(1, drawable.DuplicateInfo.DuplicateCount);
        Assert.Equal(0, DuplicateDetector.GetDuplicateGroupCount());
    }

    [Fact]
    public void CheckDrawableDuplicate_ReturnsExistingGroupWithoutRegisteringCandidate()
    {
        using var temp = new TestTempDirectory();
        var registered = CreateDrawable(temp.FilePath("registered.ydd"));
        var candidate = CreateDrawable(temp.FilePath("candidate.ydd"));
        WriteBytes(registered.FullFilePath, [1, 2, 3, 4]);
        WriteBytes(candidate.FullFilePath, [9, 8, 7, 6]);
        DuplicateDetector.RegisterDrawable(registered);

        var duplicates = DuplicateDetector.CheckDrawableDuplicate(candidate);

        Assert.NotNull(duplicates);
        Assert.Single(duplicates);
        Assert.Same(registered, duplicates[0]);
        Assert.False(candidate.DuplicateInfo.IsDuplicate);
    }

    [Fact]
    public void GetDrawablesInGroup_ReturnsRegisteredGroupAndNullForMissingGroup()
    {
        using var temp = new TestTempDirectory();
        var drawable = CreateDrawable(temp.FilePath("registered.ydd"));
        WriteBytes(drawable.FullFilePath, [1, 2, 3, 4]);
        DuplicateDetector.RegisterDrawable(drawable);

        var group = DuplicateDetector.GetDrawablesInGroup(drawable.DuplicateInfo.DuplicateGroupId);

        Assert.Single(group);
        Assert.Same(drawable, group[0]);
        Assert.Null(DuplicateDetector.GetDrawablesInGroup(null!));
        Assert.Null(DuplicateDetector.GetDrawablesInGroup("missing"));
    }

    [Fact]
    public void CheckDrawableDuplicatesBatch_ReturnsOnlyItemsThatAlreadyHaveDuplicates()
    {
        using var temp = new TestTempDirectory();
        var registered = CreateDrawable(temp.FilePath("registered.ydd"));
        var duplicate = CreateDrawable(temp.FilePath("duplicate.ydd"));
        var unique = CreateDrawable(temp.FilePath("unique.ydd"), typeNumeric: (int)ComponentNumbers.feet);
        WriteBytes(registered.FullFilePath, [1, 2, 3, 4]);
        WriteBytes(duplicate.FullFilePath, [5, 6, 7, 8]);
        WriteBytes(unique.FullFilePath, [1, 2, 3, 4]);
        DuplicateDetector.RegisterDrawable(registered);

        var result = DuplicateDetector.CheckDrawableDuplicatesBatch([duplicate, unique]);

        Assert.True(result.ContainsKey(duplicate));
        Assert.False(result.ContainsKey(unique));
        Assert.Same(registered, result[duplicate][0]);
    }

    [Fact]
    public void CheckDrawableDuplicatesBatch_ReturnsEmptyDictionaryForNullInput()
    {
        Assert.Empty(DuplicateDetector.CheckDrawableDuplicatesBatch(null!));
    }

    [Fact]
    public void UnregisterDrawable_RemovesDrawableAndRefreshesRemainingGroup()
    {
        using var temp = new TestTempDirectory();
        var first = CreateDrawable(temp.FilePath("first.ydd"));
        var second = CreateDrawable(temp.FilePath("second.ydd"));
        WriteBytes(first.FullFilePath, [1, 2, 3, 4]);
        WriteBytes(second.FullFilePath, [5, 6, 7, 8]);
        DuplicateDetector.RegisterDrawable(first);
        DuplicateDetector.RegisterDrawable(second);

        DuplicateDetector.UnregisterDrawable(second);

        Assert.False(first.DuplicateInfo.IsDuplicate);
        Assert.Equal(1, first.DuplicateInfo.DuplicateCount);
        Assert.False(second.DuplicateInfo.IsDuplicate);
        Assert.Null(second.DuplicateInfo.DuplicateGroupId);
        Assert.Equal(0, DuplicateDetector.GetDuplicateGroupCount());
    }

    [Fact]
    public void ComputeDrawableHash_ReturnsNullForUnsupportedDrawables()
    {
        using var temp = new TestTempDirectory();
        var missingFile = CreateDrawable(temp.FilePath("missing.ydd"));
        var reserved = CreateDrawable(temp.FilePath("reserved.ydd"));
        var encrypted = CreateDrawable(temp.FilePath("encrypted.ydd"));
        reserved.IsReserved = true;
        encrypted.IsEncrypted = true;
        WriteBytes(reserved.FullFilePath, [1, 2, 3, 4]);
        WriteBytes(encrypted.FullFilePath, [1, 2, 3, 4]);

        Assert.Null(DuplicateDetector.ComputeDrawableHash(null!));
        Assert.Null(DuplicateDetector.ComputeDrawableHash(missingFile));
        Assert.Null(DuplicateDetector.ComputeDrawableHash(reserved));
        Assert.Null(DuplicateDetector.ComputeDrawableHash(encrypted));
    }

    private static GDrawable CreateDrawable(string filePath, int typeNumeric = (int)ComponentNumbers.jbib)
    {
        return new GDrawable(
            Guid.NewGuid(),
            filePath,
            SexType.male,
            isProp: false,
            typeNumeric,
            number: 0,
            hasSkin: false,
            new ObservableCollection<GTexture>());
    }

    private static void WriteBytes(string filePath, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllBytes(filePath, bytes);
    }
}
