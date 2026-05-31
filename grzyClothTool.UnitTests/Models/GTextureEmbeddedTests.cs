using grzyClothTool.Models.Texture;

namespace grzyClothTool.UnitTests.Models;

public class GTextureEmbeddedTests
{
    [Fact]
    public void DefaultConstructor_CreatesMissingTextureDetails()
    {
        var texture = new GTextureEmbedded();

        Assert.False(texture.HasOriginalTexture);
        Assert.Equal(string.Empty, texture.OriginalName);
        Assert.NotNull(texture.Details);
        Assert.True(texture.IsPreviewDisabled);
        Assert.Equal("Encrypted drawable", texture.PreviewDisabledTooltip);
    }

    [Fact]
    public async Task EnsureTextureDataLoadedAsync_ReturnsFalseWhenSourceIsUnavailable()
    {
        var texture = new GTextureEmbedded
        {
            HasOriginalTexture = true,
            SourceDrawablePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd")
        };

        Assert.False(await texture.EnsureTextureDataLoadedAsync());
    }

    [Fact]
    public void RenameTexture_IgnoresBlankNamesWithoutDisplayTextureData()
    {
        var texture = new GTextureEmbedded();

        texture.RenameTexture("renamed");
        texture.RenameTexture(" ");

        Assert.Equal(string.Empty, texture.Details.Name);
    }
}
