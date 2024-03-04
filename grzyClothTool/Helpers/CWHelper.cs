using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Models;
using System.IO;

namespace grzyClothTool.Helpers;
public static class CWHelper
{
    public static CustomPedsForm CWForm;
    public static string GTAVPath => GTAFolder.GetCurrentGTAFolderWithTrailingSlash();

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

        CWForm = new CustomPedsForm();
    }

    public static void SetGTAFolder(string path)
    {
        GTAFolder.SetGTAFolder(path);
    }

    public static void GetYtdFile(string path)
    {
        //var ytd = new YtdFile();
        //ytd.Load(File.ReadAllBytes(path));
        //return ytd;
    }

    public static GTextureDetails GetTextureDetails(YtdFile ytd)
    {
        var details = new GTextureDetails();

        //todo: i think it shouldn't assume there will be only one texture, but let's leave it for now
        var txt = ytd.TextureDict.Textures[0];

        details.Format = txt.Format.ToString();
        details.MipMapCount = txt.Levels;
        details.Width = txt.Width;
        details.Height = txt.Height;

        return details;
    }
}
