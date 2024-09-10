using System;

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

    private string _hash = "";
    public string Hash
    {
        get => _hash;
        set
        {
            value ??= "";
            _hash = value.PadRight(8, '0');
            HashColor = "#FF" + (Convert.ToInt64(_hash[..8], 16) & 0xEFEFEF).ToString("X6");
        }
    }

    private string _hashColor = "#FFFF0000";
    public string HashColor
    {
        get => _hashColor;
        set
        {
            _hashColor = value;
        }
    }

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

        if (MipMapCount == 1)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "Texture has only 1 mip map. Optimize it to automatically generate correct amount.";
        }
    }
}
