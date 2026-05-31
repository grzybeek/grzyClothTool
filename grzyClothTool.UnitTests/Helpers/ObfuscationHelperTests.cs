using grzyClothTool.Helpers;

namespace grzyClothTool.UnitTests.Helpers;

public class ObfuscationHelperTests
{
    [Fact]
    public void HashString_ReturnsUppercaseSha256Hash()
    {
        var hash = ObfuscationHelper.HashString("grzyClothTool");

        Assert.Equal("CE6901E6E9DCCF8EB4BC50664BB5A39AD0CA9148D684EDEB422937FD0B848DFA", hash);
    }

    [Fact]
    public async Task XORFile_ObfuscatesAndCanRoundTripFileContent()
    {
        using var temp = new TestTempDirectory();
        var sourcePath = temp.FilePath("source.bin");
        var obfuscatedPath = temp.FilePath("obfuscated.bin");
        var restoredPath = temp.FilePath("restored.bin");
        var originalBytes = Enumerable.Range(0, 512).Select(i => (byte)(i % 256)).ToArray();
        await File.WriteAllBytesAsync(sourcePath, originalBytes);

        await ObfuscationHelper.XORFile(sourcePath, obfuscatedPath);
        await ObfuscationHelper.XORFile(obfuscatedPath, restoredPath);

        Assert.NotEqual(originalBytes, await File.ReadAllBytesAsync(obfuscatedPath));
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(restoredPath));
    }
}
