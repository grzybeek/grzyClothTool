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

        GDrawable selectedDrawable = MainWindow.AddonManager.SelectedAddon.SelectedDrawable;
        GTexture selectedTexture = MainWindow.AddonManager.SelectedAddon.SelectedTexture;

        // Don't send anything if no drawable selected
        if (selectedDrawable == null) return;

        var ydd = CreateYddFile(selectedDrawable);
        YtdFile ytd = null;
        if (selectedTexture != null)
        {
            ytd = CreateYtdFile(selectedTexture, selectedTexture.DisplayName);
            CWForm.LoadedTexture = ytd.TextureDict;
        }

        var firstDrawable = ydd.Drawables.FirstOrDefault();
        if (firstDrawable == null) return;

        CWForm.LoadedDrawable = firstDrawable;
        CWForm.Refresh();

        Dictionary<string, string> updateDict = [];
        string updateName, value;

        if (args is DrawableUpdatedArgs dargs)
        {
            updateName = dargs.UpdatedName;
            value = dargs.Value.ToString();
            updateDict.Add(updateName, value);
        }

        if (PrevDrawableSex != selectedDrawable.Sex)
        {
            SetPedModel(selectedDrawable.Sex);
            updateDict.Add("GenderChanged", "");
        }

        CWForm.UpdateSelectedDrawable(firstDrawable, ytd?.TextureDict, updateDict);
    }
}
