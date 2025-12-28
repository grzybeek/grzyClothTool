using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace grzyClothTool.Models.Duplicate;

public class DuplicateInfo : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private object _ownerItem;
    
    public void SetOwner(object owner)
    {
        _ownerItem = owner;
        OnPropertyChanged(nameof(DuplicateTooltip));
    }

    private string _duplicateGroupId;
    public string DuplicateGroupId
    {
        get => _duplicateGroupId;
        set
        {
            if (_duplicateGroupId != value)
            {
                _duplicateGroupId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDuplicate));
                OnPropertyChanged(nameof(DuplicateColor));
            }
        }
    }

    private int _duplicateCount;
    public int DuplicateCount
    {
        get => _duplicateCount;
        set
        {
            if (_duplicateCount != value)
            {
                _duplicateCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDuplicate));
                OnPropertyChanged(nameof(DuplicateColor));
                OnPropertyChanged(nameof(DuplicateTooltip));
            }
        }
    }

    public bool IsDuplicate => !string.IsNullOrEmpty(DuplicateGroupId) && DuplicateCount > 1;

    public string DuplicateTooltip
    {
        get
        {
            if (!IsDuplicate || _ownerItem == null)
                return $"Duplicate ({DuplicateCount} total)";

            var duplicates = GetAllDuplicatesForOwner();
            if (duplicates == null || duplicates.Count <= 1)
                return $"Duplicate ({DuplicateCount} total)";

            return GenerateDuplicateTooltip(duplicates);
        }
    }

    private List<object> GetAllDuplicatesForOwner()
    {
        if (_ownerItem is Drawable.GDrawable)
        {
            var hash = DuplicateGroupId;
            return Helpers.DuplicateDetector.GetDrawablesInGroup(hash)?.Cast<object>().ToList();
        }
        return null;
    }

    private string GenerateDuplicateTooltip(List<object> duplicates)
    {
        var lines = new List<string> { "Duplicated item:" };

        foreach (var duplicate in duplicates)
        {
            var isCurrent = ReferenceEquals(duplicate, _ownerItem);
            var location = GetItemLocation(duplicate);
            var sex = GetItemSex(duplicate);
            var marker = isCurrent ? " (this)" : "";
            lines.Add($"  [{sex}] {location}{marker}");
        }

        return string.Join("\n", lines);
    }

    private static string GetItemSex(object item)
    {
        if (item is Drawable.GDrawable drawable)
        {
            return drawable.SexName ?? "Unknown";
        }
        return "Unknown";
    }

    public string DuplicateColor
    {
        get
        {
            if (!IsDuplicate || string.IsNullOrEmpty(DuplicateGroupId))
                return "Transparent";
            
            try
            {
                var hashBytes = Convert.FromBase64String(DuplicateGroupId);
                return $"#FF{(hashBytes[0] & 0xEF):X2}{(hashBytes[1] & 0xEF):X2}{(hashBytes[2] & 0xEF):X2}";
            }
            catch
            {
                return "#FF9CA3AF";
            }
        }
    }

    private static string GetItemLocation(object item)
    {
        if (MainWindow.AddonManager?.Addons == null)
            return "Unknown";

        var addons = MainWindow.AddonManager.Addons;
        
        for (int i = 0; i < addons.Count; i++)
        {
            var addon = addons[i];
            
            if (item is Drawable.GDrawable drawable)
            {
                if (addon.Drawables.Contains(drawable))
                {
                    return $"Addon {i + 1}: {drawable.Name}";
                }
            }
            else if (item is Texture.GTexture texture)
            {
                foreach (var draw in addon.Drawables)
                {
                    if (draw.Textures.Contains(texture))
                    {
                        return $"Addon {i + 1}: {draw.Name} → {texture.DisplayName}";
                    }
                }
            }
        }

        return "Unknown";
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
