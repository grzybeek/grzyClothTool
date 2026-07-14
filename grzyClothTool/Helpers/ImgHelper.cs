using CodeWalker.GameFiles;
using grzyClothTool.Models.Texture;
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

    public static MagickImage GetImage(string path)
    {
        string ext = Path.GetExtension(path);

        try
        {
            if(ext == ".ytd")
            {
                var ytd = CWHelper.GetYtdFile(path);

                if (ytd.TextureDict.Textures.Count == 0)
                {
                    return null;
                }
                var txt = ytd.TextureDict.Textures[0];
                var dds = CodeWalker.Utils.DDSIO.GetDDSFile(txt);

                return new MagickImage(dds);
            }
            else
            {
                return new MagickImage(path);
            }
        }
        catch (Exception e) when (e.Message.Contains("Invalid slice pitch"))
        {
            TelemetryHelper.CaptureExceptionWithAttachment(e, path);
            throw;
        }
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

    public static async Task<byte[]?> Optimize(GTexture gtxt, bool shouldSkipOptimization = false)
    {
        try
        {
            if (shouldSkipOptimization && gtxt.Extension == ".dds")
            {
                return GetDDSBytes(gtxt);
            }

            var ytd = new YtdFile
            {
                TextureDict = new TextureDictionary()
            };

            using var img = GetImage(gtxt.FullFilePath);
            img.Format = MagickFormat.Dds;

            // Skip optimization (I think this is best way to not duplicate code, and reuse this for jpg/png textures that don't need optimization)
            if (!shouldSkipOptimization)
            {
                var details = gtxt.OptimizeDetails;
                ResizeExact(img, details.Width, details.Height, details.Compression);
                img.Settings.SetDefine(MagickFormat.Dds, "compression", GetCompressionString(details.Compression));
                img.Settings.SetDefine(MagickFormat.Dds, "cluster-fit", true);
                img.Settings.SetDefine(MagickFormat.Dds, "mipmaps", details.MipMapCount);
            }

            var stream = new MemoryStream();
            img.Write(stream);

            var newDds = stream.ToArray();
            var newTxt = CodeWalker.Utils.DDSIO.GetTexture(newDds);
            newTxt.Name = gtxt.DisplayName;
            ytd.TextureDict.BuildFromTextureList([newTxt]);

            var bytes = ytd.Save();
            return bytes;
        }
        catch (MagickCorruptImageErrorException)
        {
            // Image is corrupted and cannot be processed
            return null;
        }
    }

    public static async Task<byte[]?> Optimize(byte[] imgBytes, GTextureDetails optimizeDetails)
    {
        try
        {
            using var img = new MagickImage(imgBytes);
            img.Format = MagickFormat.Dds;

            ResizeExact(img, optimizeDetails.Width, optimizeDetails.Height, optimizeDetails.Compression);
            img.Settings.SetDefine(MagickFormat.Dds, "compression", GetCompressionString(optimizeDetails.Compression));
            img.Settings.SetDefine(MagickFormat.Dds, "cluster-fit", true);
            img.Settings.SetDefine(MagickFormat.Dds, "mipmaps", optimizeDetails.MipMapCount);

            var stream = new MemoryStream();
            img.Write(stream);
            return stream.ToArray();
        }
        catch (MagickCorruptImageErrorException)
        {
            // Image is corrupted and cannot be processed
            return null;
        }
    }

    public static byte[] GetDDSBytes(GTexture gtxt)
    {
        var ytd = new YtdFile
        {
            TextureDict = new TextureDictionary()
        };

        byte[] ddsBytes = [];

        if (gtxt.Extension == ".dds")
        {
            ddsBytes = File.ReadAllBytes(gtxt.FullFilePath);
        } 
        else if (gtxt.Extension == ".jpg" || gtxt.Extension == ".png")
        {
            using var img = GetImage(gtxt.FullFilePath);
            img.Format = MagickFormat.Dds;

            var stream = new MemoryStream();
            img.Write(stream);

            ddsBytes = stream.ToArray();
        }

        var newTxt = CodeWalker.Utils.DDSIO.GetTexture(ddsBytes);

        newTxt.Name = gtxt.DisplayName;
        ytd.TextureDict.BuildFromTextureList([newTxt]);

        return ytd.Save();
    }

    public static byte[] GetDDSFileBytes(GTexture gtxt)
    {
        if (gtxt.Extension == ".dds")
        {
            return File.ReadAllBytes(gtxt.FullFilePath);
        }

        if (gtxt.Extension == ".ytd")
        {
            var ytd = new YtdFile();
            ytd.Load(File.ReadAllBytes(gtxt.FullFilePath));
            if (ytd.TextureDict.Textures.Count == 0)
            {
                return [];
            }

            return CodeWalker.Utils.DDSIO.GetDDSFile(ytd.TextureDict.Textures[0]);
        }

        if (gtxt.Extension == ".jpg" || gtxt.Extension == ".png")
        {
            using var img = GetImage(gtxt.FullFilePath);
            img.Format = MagickFormat.Dds;

            var stream = new MemoryStream();
            img.Write(stream);

            return stream.ToArray();
        }

        throw new NotSupportedException($"Unsupported file extension: {gtxt.Extension}");
    }

    private static string GetCompressionString(string cwCompression)
    {
        return cwCompression switch
        {
            "D3DFMT_DXT1" => "dxt1",
            "D3DFMT_DXT3" => "dxt3",
            "D3DFMT_DXT5" => "dxt5",
            "D3DFMT_A8R8G8B8" => "none",
            _ => "dxt5",
        };
    }

    private static bool IsBlockCompressed(string cwCompression) => cwCompression switch
    {
        "D3DFMT_DXT1" or "D3DFMT_DXT3" or "D3DFMT_DXT5"
        or "D3DFMT_ATI1" or "D3DFMT_ATI2" or "D3DFMT_BC7" => true,
        _ => false,
    };

    private static int RoundDownToMultipleOf4(int value) => Math.Max(4, value - (value % 4));

    private static void ResizeExact(MagickImage img, int width, int height, string cwCompression)
    {
        if (IsBlockCompressed(cwCompression))
        {
            width = RoundDownToMultipleOf4(width);
            height = RoundDownToMultipleOf4(height);
        }

        var geometry = new MagickGeometry((uint)width, (uint)height) { IgnoreAspectRatio = true };
        img.Resize(geometry);
    }
}
