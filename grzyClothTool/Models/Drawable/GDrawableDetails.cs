using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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

    public Dictionary<EmbeddedTextureType, GTextureEmbedded?> EmbeddedTextures { get; set; } = new()
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

    private bool _hasTextureWarnings;
    public bool HasTextureWarnings
    {
        get => _hasTextureWarnings;
        set
        {
            _hasTextureWarnings = value;
            OnPropertyChanged(nameof(HasTextureWarnings));
        }
    }

    private bool _hasEmbeddedTextureWarnings;
    public bool HasEmbeddedTextureWarnings
    {
        get => _hasEmbeddedTextureWarnings;
        set
        {
            _hasEmbeddedTextureWarnings = value;
            OnPropertyChanged(nameof(HasEmbeddedTextureWarnings));
        }
    }

    private bool _hasHighHeelsWarning;
    public bool HasHighHeelsWarning
    {
        get => _hasHighHeelsWarning;
        set
        {
            _hasHighHeelsWarning = value;
            OnPropertyChanged(nameof(HasHighHeelsWarning));
        }
    }

    public bool ShouldCheckHighHeels { get; set; }

    public void Validate(
        ObservableCollection<GTexture>? textures = null,
        bool enableHighHeels = false,
        float highHeelsValue = 0,
        bool ignoreWarnings = false)
    {
        // reset values
        Tooltip = string.Empty;
        IsWarning = false;
        HasTextureWarnings = false;
        HasEmbeddedTextureWarnings = false;
        HasHighHeelsWarning = false;

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
            if (txt == null || !txt.HasOriginalTexture)
            {
                IsWarning = true;
                Tooltip += $"Missing {key} texture.\n";
                continue;
            }
            
            if (txt.Details.IsOptimizeNeeded)
            {
                HasEmbeddedTextureWarnings = true;
            }
        }

        if (TexturesCount == 0)
        {
            IsWarning = true;
            Tooltip += "Drawable has no textures.\n";
        }
        
        if (textures != null && textures.Count > 0)
        {
            var texturesWithWarnings = textures
                .Where(t => t.TxtDetails != null && t.TxtDetails.IsOptimizeNeeded)
                .ToList();
            
            if (texturesWithWarnings.Count > 0)
            {
                HasTextureWarnings = true;
                IsWarning = true;
            }
        }
        
        var embeddedTexturesWithWarnings = EmbeddedTextures.Values
            .Where(et => et != null && et.HasOriginalTexture && et.Details.IsOptimizeNeeded)
            .Any();
            
        if (embeddedTexturesWithWarnings)
        {
            HasEmbeddedTextureWarnings = true;
        }
        
        if (HasTextureWarnings || HasEmbeddedTextureWarnings)
        {
            Tooltip += "Some textures have warnings. Check texture details.\n";
            IsWarning = true;
        }

        if (ShouldCheckHighHeels && !enableHighHeels)
        {
            HasHighHeelsWarning = true;
            IsWarning = true;
            Tooltip += "Shoes might be under floor. Consider enabling High heels and validating manually in the 3D preview.\n";
        }

        // Remove trailing newline character
        Tooltip = Tooltip.TrimEnd('\n');

        if (ignoreWarnings && IsWarning)
        {
            Tooltip = string.Empty;
            IsWarning = false;
            HasTextureWarnings = false;
            HasEmbeddedTextureWarnings = false;
            HasHighHeelsWarning = false;
        }
    }

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GDrawableModel
{
    public int PolyCount { get; set; }
}
