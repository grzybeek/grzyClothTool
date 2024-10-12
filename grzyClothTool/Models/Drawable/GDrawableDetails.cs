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

    public int TexturesCount = 0;

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
                IsWarning = true;
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
                IsWarning = true;
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

        if (TexturesCount == 0)
        {
            IsWarning = true;
            Tooltip += "Drawable has no textures.\n";
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