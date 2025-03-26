using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace grzyClothTool.Helpers;
public static class CWHelper
{
    public static CustomPedsForm CWForm;
    public static string GTAVPath => GTAFolder.GetCurrentGTAFolderWithTrailingSlash();
    public static bool IsCacheStartupEnabled => Properties.Settings.Default.GtaCacheStartup;

    private static readonly YtdFile _ytdFile = new();

    private static Enums.SexType PrevDrawableSex;

    public static void Init()
    {
        var isFolderValid = GTAFolder.IsCurrentGTAFolderValid();
        if(!isFolderValid)
        {
            var folder = GTAFolder.AutoDetectFolder();
            if (folder != null)
            {
                SetGTAFolder(folder);
            }
        }

        if (IsCacheStartupEnabled)
        {
            // todo: load cw cache
        }

        CWForm = new CustomPedsForm();
    }

    public static void SetGTAFolder(string path)
    {
        GTAFolder.SetGTAFolder(path);
    }

    public static void SetCacheStartup(bool value)
    {
        Properties.Settings.Default.GtaCacheStartup = value;
        Properties.Settings.Default.Save();
    }

    public static YtdFile GetYtdFile(string path)
    {
        _ytdFile.Load(File.ReadAllBytes(path));
        return _ytdFile;
    }

    public static YtdFile CreateYtdFile(GTexture texture, string name)
    {
        byte[] data = texture.Extension switch
        {
            ".ytd" => File.ReadAllBytes(texture.FilePath), // Read existing YTD file directly
            ".png" or ".jpg" or ".dds" => ImgHelper.GetDDSBytes(texture), // Create DDS texture
            _ => throw new NotSupportedException($"Unsupported file extension: {texture.Extension}"),
        };

        RpfFileEntry rpf = RpfFile.CreateResourceFileEntry(ref data, 0);
        var decompressedData = ResourceBuilder.Decompress(data);
        YtdFile ytd = RpfFile.GetFile<YtdFile>(rpf, decompressedData);
        ytd.Name = Path.GetFileNameWithoutExtension(name);

        return ytd;
    }

    public static YddFile CreateYddFile(GDrawable d)
    {
        try
        {
            byte[] data = File.ReadAllBytes(d.FilePath);

            RpfFileEntry rpf = RpfFile.CreateResourceFileEntry(ref data, 0);
            var decompressedData = ResourceBuilder.Decompress(data);
            YddFile ydd = RpfFile.GetFile<YddFile>(rpf, decompressedData);
            var drawable = ydd.Drawables.First();
            drawable.Name = Path.GetFileNameWithoutExtension(d.Name);

            drawable.IsHairScaleEnabled = d.EnableHairScale;
            if (drawable.IsHairScaleEnabled)
            {
                drawable.HairScaleValue = d.HairScaleValue;
            }

            drawable.IsHighHeelsEnabled = d.EnableHighHeels;
            if (drawable.IsHighHeelsEnabled)
            {
                drawable.HighHeelsValue = d.HighHeelsValue / 10;
            }

            return ydd;
        }
        catch (Exception ex)
        {
            TelemetryHelper.CaptureExceptionWithAttachment(ex, d.FilePath);
            throw;
        }
    }

    public static void SetPedModel(Enums.SexType sexType)
    {
        string pedModel = sexType == Enums.SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
        PrevDrawableSex = sexType;
        CWForm.PedModel = pedModel;
    }

    public static void SendDrawableUpdateToPreview(EventArgs args)
    {
        // MainWindow.AddonManager.IsPreviewEnabled is still true, but preview window is closed already
        // It causes deadlock if user is fast enough to select different drawable. Check if form is open
        if (!CWForm.formopen || CWForm.isLoading) return;

        var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables;

        // Don't send anything if no drawables are selected
        if (selectedDrawables.Count == 0) return;

        Dictionary<string, string> updateDict = [];
        if (args is DrawableUpdatedArgs dargs)
        {
            updateDict[dargs.UpdatedName] = dargs.Value.ToString();
        }

        // Identify drawables that are no longer selected and remove them
        var selectedNames = selectedDrawables.Select(d => d.Name).ToHashSet();
        var removedDrawables = CWForm.LoadedDrawables.Keys.Where(name => !selectedNames.Contains(name)).ToList();
        foreach (var removed in removedDrawables)
        {

            if (CWForm.LoadedDrawables.TryGetValue(removed, out var removedDrawable))
            {
                CWForm.LoadedTextures.Remove(removedDrawable);
            }
            CWForm.LoadedDrawables.Remove(removed);
        }

        if (selectedDrawables.Count == 1)
        {
            var firstSelected = selectedDrawables.First();
            if (PrevDrawableSex != firstSelected.Sex)
            {
                SetPedModel(firstSelected.Sex);
                updateDict.Add("GenderChanged", "");
            }
        }

        // Add or update selected drawables and their textures
        foreach (var drawable in selectedDrawables)
        {
            var ydd = CreateYddFile(drawable);
            if (ydd == null || ydd.Drawables.Length == 0) continue;

            var firstDrawable = ydd.Drawables.First();
            CWForm.LoadedDrawables[drawable.Name] = firstDrawable;

            GTexture selectedTexture = MainWindow.AddonManager.SelectedAddon.SelectedTexture;
            YtdFile ytd = null;
            if (selectedTexture != null)
            {
                ytd = CreateYtdFile(selectedTexture, selectedTexture.DisplayName);
                CWForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
            }

            if (selectedTexture == null && selectedDrawables.Count > 1)
            {
                // If multiple drawables are selected, we need to load the first texture of the first drawable
                // to prevent the preview from being empty
                var firstTexture = drawable.Textures.FirstOrDefault();
                if (firstTexture != null)
                {
                    ytd = CreateYtdFile(firstTexture, firstTexture.DisplayName);
                    CWForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                }
            }

            CWForm.UpdateSelectedDrawable(
                firstDrawable,
                ytd.TextureDict,
                updateDict
            );
        }
    }
}
