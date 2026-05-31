using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;

namespace grzyClothTool.UnitTests.Models;

public class GDrawableDetailsTests
{
    [Fact]
    public void Validate_WarnsForMissingModelsTexturesAndEmbeddedTextures()
    {
        var details = new GDrawableDetails();

        details.Validate();

        Assert.True(details.IsWarning);
        Assert.Contains("Missing LOD model", details.Tooltip);
        Assert.Contains("Missing Specular texture", details.Tooltip);
        Assert.Contains("Drawable has no textures", details.Tooltip);
    }

    [Fact]
    public void Validate_ClearsWarningsWhenIgnoreWarningsIsTrue()
    {
        var details = new GDrawableDetails();

        details.Validate(ignoreWarnings: true);

        Assert.False(details.IsWarning);
        Assert.False(details.HasTextureWarnings);
        Assert.False(details.HasEmbeddedTextureWarnings);
        Assert.Equal(string.Empty, details.Tooltip);
    }

    [Fact]
    public void Validate_WarnsWhenHighHeelCheckIsRecommendedButDisabled()
    {
        var details = new GDrawableDetails
        {
            TexturesCount = 1,
            ShouldCheckHighHeels = true
        };
        FillModelsWithinLimits(details);

        details.Validate(enableHighHeels: false);

        Assert.True(details.HasHighHeelsWarning);
        Assert.Contains("High heels", details.Tooltip);
    }

    [Fact]
    public void Validate_DoesNotWarnForCompleteDetailsWithinLimits()
    {
        var details = new GDrawableDetails { TexturesCount = 1 };
        FillModelsWithinLimits(details);
        FillEmbeddedTexturesWithoutWarnings(details);

        details.Validate();

        Assert.False(details.IsWarning);
        Assert.Equal(string.Empty, details.Tooltip);
    }

    [Fact]
    public void Validate_WarnsWhenModelExceedsConfiguredPolygonLimit()
    {
        var details = new GDrawableDetails { TexturesCount = 1 };
        FillModelsWithinLimits(details);
        FillEmbeddedTexturesWithoutWarnings(details);
        details.AllModels[GDrawableDetails.DetailLevel.High] = new GDrawableModel
        {
            PolyCount = SettingsHelper.Instance.PolygonLimitHigh + 1
        };

        details.Validate();

        Assert.True(details.IsWarning);
        Assert.Contains("Polygon count", details.Tooltip);
    }

    private static void FillModelsWithinLimits(GDrawableDetails details)
    {
        details.AllModels[GDrawableDetails.DetailLevel.High] = new GDrawableModel { PolyCount = 0 };
        details.AllModels[GDrawableDetails.DetailLevel.Med] = new GDrawableModel { PolyCount = 0 };
        details.AllModels[GDrawableDetails.DetailLevel.Low] = new GDrawableModel { PolyCount = 0 };
    }

    private static void FillEmbeddedTexturesWithoutWarnings(GDrawableDetails details)
    {
        foreach (var textureType in details.EmbeddedTextures.Keys.ToList())
        {
            details.EmbeddedTextures[textureType] = new TestEmbeddedTexture();
        }
    }

    private sealed class TestEmbeddedTexture : grzyClothTool.Models.Texture.GTextureEmbedded
    {
        public TestEmbeddedTexture()
        {
            HasOriginalTexture = true;
            Details = new grzyClothTool.Models.Texture.GTextureDetails
            {
                Width = 512,
                Height = 512,
                MipMapCount = 10,
                Compression = "Dxt1"
            };
        }
    }
}
