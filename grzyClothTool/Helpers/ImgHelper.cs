using grzyClothTool.Models;
using ImageMagick;
using System;
using System.IO;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

public static class ImgHelper
{
    static ImgHelper()
    {
        MagickNET.Initialize();
    }

    public static int GetCorrectMipMapAmount(int width, int height)
    {
        int size = Math.Min(width, height);
        return (int)Math.Log(size, 2) - 1;
    }

    public static (int, int) CheckPowerOfTwo(int width, int height)
    {
        if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
        {
            int newWidth = (int)Math.Pow(2, Math.Ceiling(Math.Log(width) / Math.Log(2)));
            int newHeight = (int)Math.Pow(2, Math.Ceiling(Math.Log(height) / Math.Log(2)));

            return (newWidth, newHeight);
        }
        return (width, height);
    }

    public static async Task OptimizeAndSave(GTexture gtxt, string savePath)
    {
        var ytd = CWHelper.GetYtdFile(gtxt.FilePath);
        var txt = ytd.TextureDict.Textures[0];
        var dds = CodeWalker.Utils.DDSIO.GetDDSFile(txt);

        var details = gtxt.TxtDetails;

        using var img = new MagickImage(dds);

        img.Resize(details.Width, details.Height);
        img.Settings.SetDefine(MagickFormat.Dds, "compression", GetCompressionString(details.Compression));
        img.Settings.SetDefine(MagickFormat.Dds, "cluster-fit", true);
        img.Settings.SetDefine(MagickFormat.Dds, "mipmaps", details.MipMapCount);

        var stream = new MemoryStream();
        img.Write(stream);

        var newDds = stream.ToArray();
        var newTxt = CodeWalker.Utils.DDSIO.GetTexture(newDds);
        newTxt.Name = txt.Name;
        ytd.TextureDict.BuildFromTextureList([newTxt]);

        var bytes = ytd.Save();
        await File.WriteAllBytesAsync(savePath, bytes);
    }

    private static string GetCompressionString(string cwCompression)
    {
        return cwCompression switch
        {
            "D3DFMT_DXT1" => "dxt1",
            "D3DFMT_DXT3" => "dxt3",
            "D3DFMT_DXT5" => "dxt5",
            _ => "dxt5",
        };
    }
}
