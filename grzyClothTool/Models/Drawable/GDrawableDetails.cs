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

    public Dictionary<DetailLevel, GDrawableModel?> AllModels { get; set; } = new()
    {
        { DetailLevel.High, null },
        { DetailLevel.Med, null },
        { DetailLevel.Low, null }
    };

    public List<GTextureDetails> EmbeddedTextures { get; set; } = [];

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
                Tooltip += $"Missing {detailLevel} LOD model.\n";
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
                Tooltip += $"[{detailLevel}] Polygon count exceeds the limit of {polygonLimit}.\n";
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