using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace grzyClothTool.Helpers;
public static class CWHelper
{
    public static PreviewWindowHost DockedPreviewHost;
    public static string GTAVPath => GTAFolder.GetCurrentGTAFolderWithTrailingSlash();

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
    }

    public static void SetGTAFolder(string path)
    {
        GTAFolder.SetGTAFolder(path);
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
        DockedPreviewHost?.SetPedModel(pedModel);
    }

    public static void SendDrawableUpdateToPreview(EventArgs args)
    {
        if (DockedPreviewHost == null)
        {
            return;
        }

        var selectedDrawables = MainWindow.AddonManager.SelectedAddon.SelectedDrawables;

        // Don't send anything if no drawables are selected
        if (selectedDrawables.Count == 0) return;

        Dictionary<string, string> updateDict = [];
        if (args is DrawableUpdatedArgs dargs)
        {
            updateDict[dargs.UpdatedName] = dargs.Value.ToString();
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

        DockedPreviewHost.UpdateDrawables(selectedDrawables, MainWindow.AddonManager.SelectedAddon.SelectedTexture, updateDict);
    }

    public static void OpenDrawableInPreview(GDrawable drawable)
    {
        if (drawable == null)
            return;

        if (!MainWindow.AddonManager.SelectedAddon.SelectedDrawables.Contains(drawable))
        {
            MainWindow.AddonManager.SelectedAddon.SelectedDrawables.Clear();
            MainWindow.AddonManager.SelectedAddon.SelectedDrawables.Add(drawable);
            MainWindow.AddonManager.SelectedAddon.SelectedDrawable = drawable;
        }

        var mainWindow = MainWindow.Instance;
        if (mainWindow == null) return;

        if (mainWindow.PreviewAnchorable != null)
        {
            mainWindow.PreviewAnchorable.Show();
            mainWindow.PreviewHost?.InitializePreview();

            if (!drawable.IsEncrypted)
            {
                SendDrawableUpdateToPreview(new RoutedEventArgs());
            }

            MainWindow.AddonManager.IsPreviewEnabled = true;
        }
    }
}
