using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Models;
using System.IO;

namespace grzyClothTool.Helpers;
public static class CWHelper
{
    public static CustomPedsForm CWForm;
    public static string GTAVPath => GTAFolder.GetCurrentGTAFolderWithTrailingSlash();

    private static readonly YtdFile _ytdFile = new();

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

    public static YtdFile GetYtdFile(string path)
    {
        _ytdFile.Load(File.ReadAllBytes(path));
        return _ytdFile;
    }

}
