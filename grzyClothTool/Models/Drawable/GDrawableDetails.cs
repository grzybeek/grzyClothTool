using grzyClothTool.Models.Texture;
using System.Collections.Generic;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawableDetails
{
    public enum DetailLevel
    {
        High,
        Med,
        Low
    }

    public Dictionary<DetailLevel, GDrawableModel?> AllModels { get; set; } = new()
    {
        { DetailLevel.High, null },
        { DetailLevel.Med, null },
        { DetailLevel.Low, null }
    };

    public List<GTextureDetails> EmbeddedTextures { get; set; } = [];

    public void Validate()
    {
        foreach(var model in AllModels.Values)
        {
            if (model == null)
            {
                continue;
            }

            if (model.PolyCount > 10000)
            {
                model.PolyCount = 10000;
            }
        
        }
    }
}

public class GDrawableModel
{
    public int PolyCount { get; set; }
}