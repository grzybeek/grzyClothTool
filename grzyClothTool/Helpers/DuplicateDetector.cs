using grzyClothTool.Models.Drawable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

public static class DuplicateDetector
{
    private static readonly Dictionary<string, List<GDrawable>> _drawableDuplicateGroups = new();

    public static string ComputeDrawableHash(GDrawable drawable)
    {
        if (drawable == null || drawable.IsReserved || drawable.IsEncrypted)
            return null;

        try
        {
            if (!File.Exists(drawable.FullFilePath))
                return null;

            var fileInfo = new FileInfo(drawable.FullFilePath);
            var fileSize = fileInfo.Length;

            int attempts = 0;
            while (drawable.IsLoading && attempts < 50)
            {
                Task.Delay(100).Wait();
                attempts++;
            }

            var hashComponents = new List<string>
            {
                fileSize.ToString(),
                drawable.TypeNumeric.ToString(),
                drawable.IsProp.ToString(),
                drawable.Sex.ToString()
            };

            if (drawable.Details != null)
            {
                if (drawable.Details.AllModels != null)
                {
                    foreach (var modelPair in drawable.Details.AllModels)
                    {
                        if (modelPair.Value != null)
                        {
                            hashComponents.Add($"{modelPair.Key}:{modelPair.Value.PolyCount}");
                        }
                    }
                }

                hashComponents.Add(drawable.Details.TexturesCount.ToString());
            }

            var combinedString = string.Join("|", hashComponents);
            var hashBytes = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(combinedString));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            Helpers.ErrorLogHelper.LogError($"Error computing drawable hash: {ex.Message}", ex);
            return null;
        }
    }

    public static List<GDrawable> CheckDrawableDuplicate(GDrawable drawable)
    {
        if (drawable == null || drawable.IsReserved)
            return null;

        var hash = ComputeDrawableHash(drawable);
        if (string.IsNullOrEmpty(hash))
            return null;

        if (_drawableDuplicateGroups.TryGetValue(hash, out var existingGroup))
        {
            return existingGroup;
        }

        return null;
    }

    public static Dictionary<GDrawable, List<GDrawable>> CheckDrawableDuplicatesBatch(IEnumerable<GDrawable> drawables)
    {
        var result = new Dictionary<GDrawable, List<GDrawable>>();

        if (drawables == null)
            return result;

        foreach (var drawable in drawables)
        {
            if (drawable == null || drawable.IsReserved)
                continue;

            var existingDuplicates = CheckDrawableDuplicate(drawable);
            if (existingDuplicates != null && existingDuplicates.Count > 0)
            {
                result[drawable] = existingDuplicates;
            }
        }

        return result;
    }

    public static void RegisterDrawable(GDrawable drawable)
    {
        if (drawable == null || drawable.IsReserved)
            return;

        var hash = ComputeDrawableHash(drawable);
        if (string.IsNullOrEmpty(hash))
            return;

        if (!_drawableDuplicateGroups.TryGetValue(hash, out List<GDrawable> value))
        {
            value = [];
            _drawableDuplicateGroups[hash] = value;
        }

        if (!value.Contains(drawable))
        {
            value.Add(drawable);
            UpdateDrawableDuplicateInfo(hash);
        }
    }

    public static void UnregisterDrawable(GDrawable drawable)
    {
        if (drawable == null || drawable.DuplicateInfo == null)
            return;

        var hash = drawable.DuplicateInfo.DuplicateGroupId;
        if (string.IsNullOrEmpty(hash))
            return;

        if (_drawableDuplicateGroups.TryGetValue(hash, out var group))
        {
            group.Remove(drawable);
            
            if (group.Count == 0)
            {
                _drawableDuplicateGroups.Remove(hash);
            }
            else
            {
                UpdateDrawableDuplicateInfo(hash);
            }
        }

        drawable.DuplicateInfo.DuplicateGroupId = null;
        drawable.DuplicateInfo.DuplicateCount = 0;
    }


    private static void UpdateDrawableDuplicateInfo(string hash)
    {
        if (!_drawableDuplicateGroups.TryGetValue(hash, out var group))
            return;

        var count = group.Count;
        foreach (var drawable in group)
        {
            if (drawable.DuplicateInfo.DuplicateGroupId != hash)
            {
                drawable.DuplicateInfo.DuplicateGroupId = hash;
            }

            drawable.DuplicateInfo.DuplicateCount = count;
        }
    }

    public static List<GDrawable> GetDrawablesInGroup(string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return null;

        return _drawableDuplicateGroups.TryGetValue(hash, out var group) ? group : null;
    }

    public static void Clear()
    {
        _drawableDuplicateGroups.Clear();
    }

    public static int GetDuplicateGroupCount()
    {
        return _drawableDuplicateGroups.Count(kvp => kvp.Value.Count > 1);
    }


    public static void RescanDrawables()
    {
        Clear();

        if (MainWindow.AddonManager?.Addons == null)
            return;

        foreach (var addon in MainWindow.AddonManager.Addons)
        {
            if (addon.Drawables == null)
                continue;

            foreach (var drawable in addon.Drawables)
            {
                if (drawable != null && !drawable.IsReserved)
                {
                    RegisterDrawable(drawable);
                }
            }
        }
    }
}
