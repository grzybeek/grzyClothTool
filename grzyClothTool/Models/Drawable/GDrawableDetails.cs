using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawableDetails : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public enum DetailLevel
    {
        High,
        Med,
        Low
    }

    public enum EmbeddedTextureType
    {
        Specular,
        Normal
    }

    public Dictionary<DetailLevel, GDrawableModel?> AllModels { get; set; } = new()
    {
        { DetailLevel.High, null },
        { DetailLevel.Med, null },
        { DetailLevel.Low, null }
    };

    public Dictionary<EmbeddedTextureType, GTextureDetails?> EmbeddedTextures { get; set; } = new()
    {
        { EmbeddedTextureType.Specular, null },
        { EmbeddedTextureType.Normal, null }
    };


    private bool _isWarning;
    public bool IsWarning
    {
        get => _isWarning;
        set
        {
            _isWarning = value;
            OnPropertyChanged(nameof(IsWarning));
        }
    }

    private string _tooltip = string.Empty;
    public string Tooltip
    {
        get => _tooltip;
        set
        {
            _tooltip = value;
            OnPropertyChanged(nameof(Tooltip));
        }
    }

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
        // reset values
        Tooltip = string.Empty;
        IsWarning = false;

        foreach (var detailLevel in AllModels.Keys)
        {
            var model = AllModels[detailLevel];
            if (model == null)
            {
                Tooltip += $"[{detailLevel}] Missing LOD model.\n";
                continue;
            }

            int polygonLimit = detailLevel switch
            {
                DetailLevel.High => SettingsHelper.Instance.PolygonLimitHigh,
                DetailLevel.Med => SettingsHelper.Instance.PolygonLimitMed,
                DetailLevel.Low => SettingsHelper.Instance.PolygonLimitLow,
                _ => throw new InvalidOperationException("Unknown detail level")
            };

            if (model.PolyCount > polygonLimit)
            {
                IsWarning = true;
                Tooltip += $"[{detailLevel}] Polygon count of {model.PolyCount} exceeds the limit of {polygonLimit}.\n";
            }
        }

        foreach (var key in EmbeddedTextures.Keys)
        {
            var txt = EmbeddedTextures[key];
            if (txt == null)
            {
                Tooltip += $"Missing {key} texture.\n";
                continue;
            }

            txt.Validate();
            if (txt.IsOptimizeNeeded)
            {
                IsWarning = true;

                var messages = txt.IsOptimizeNeededTooltip.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var message in messages)
                {
                    Tooltip += $"[Embedded {key}] {message}\n";
                }
            }
        }

        // Remove trailing newline character
        Tooltip = Tooltip.TrimEnd('\n');
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GDrawableModel
{
    public int PolyCount { get; set; }
}