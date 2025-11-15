using grzyClothTool.Helpers;

namespace grzyClothTool.Models.Texture;
#nullable enable

public class GTextureDetails
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MipMapCount { get; set; }
    public string Compression { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? Type { get; set; } = string.Empty;

    public bool IsOptimizeNeeded { get; set; }
    public string IsOptimizeNeededTooltip { get; set; } = string.Empty;

    public void Validate()
    {
        if (Width > 2048 || Height > 2048)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "Texture is larger than 2048x2048. Optimize it to reduce the size.\n";
        }

        if ((Height & Height - 1) != 0 || (Width & Width - 1) != 0)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "Texture height or width is not power of 2. Optimize it to fix the issue.\n";
        }

        var expectedMipMapCount = ImgHelper.GetCorrectMipMapAmount(Width, Height);
        if (MipMapCount == 1 && MipMapCount != expectedMipMapCount)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += $"Texture has {MipMapCount} mip maps but should have {expectedMipMapCount}. Optimize it to generate the correct amount.\n";
        }
    }
}
