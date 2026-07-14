using System.Collections.ObjectModel;
using System.Text.Json;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class SaveHelperSerializationTests
{
    [Fact]
    public void SerializerOptions_RoundTripsAddonManagerGraph()
    {
        var manager = new AddonManager { ProjectName = "test-project" };
        var addon = new Addon("Addon 1");

        for (int i = 0; i < 3; i++)
        {
            var texture = new GTexture(
                Guid.NewGuid(),
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd"),
                typeNumeric: 11,
                number: i,
                txtNumber: 0,
                hasSkin: false,
                isProp: false);

            addon.Drawables.Add(new GDrawable(
                Guid.NewGuid(),
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
                SexType.male,
                isProp: false,
                typeNumeric: 11,
                number: i,
                hasSkin: false,
                new ObservableCollection<GTexture> { texture }));
        }

        manager.Addons.Add(addon);

        // Serialize with the cached options used by autosave
        var json = JsonSerializer.Serialize(manager, SaveHelper.SerializerOptions);
        Assert.False(string.IsNullOrWhiteSpace(json));

        // The autosave validation step must accept the output
        using (var doc = JsonDocument.Parse(json))
        {
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        // And the load path must be able to restore the full graph
        var restored = JsonSerializer.Deserialize<AddonManager>(json, SaveHelper.SerializerOptions);
        Assert.NotNull(restored);
        Assert.Equal("test-project", restored.ProjectName);
        Assert.Single(restored.Addons);
        Assert.Equal(3, restored.Addons[0].Drawables.Count);
        Assert.Equal("jbib_000_u", restored.Addons[0].Drawables[0].Name);
        Assert.Single(restored.Addons[0].Drawables[0].Textures);
    }
}
