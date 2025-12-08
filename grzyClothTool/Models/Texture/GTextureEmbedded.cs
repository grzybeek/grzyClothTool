using CodeWalker.Utils;
using grzyClothTool.Helpers;
using ImageMagick;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTextureEmbedded : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string OriginalName;

    public GTextureDetails Details { get; set; } = new GTextureDetails();
    public GTextureDetails? OptimizeDetails { get; set; }

    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? TextureData { get; set; }

    private CodeWalker.GameFiles.Texture? _replacementTextureData;
    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? ReplacementTextureData
    {
        get => _replacementTextureData;
        set
        {
            _replacementTextureData = value;
            
            if (value != null)
            {
                IsOptimizedDuringBuild = false;
                OptimizeDetails = null;
            }
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReplacement));
            OnPropertyChanged(nameof(DisplayTextureData));
            OnPropertyChanged(nameof(IsOptimizedDuringBuild));
        }
    }

    [JsonIgnore]
    public bool HasReplacement => _replacementTextureData != null;

    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? DisplayTextureData => _replacementTextureData ?? TextureData;

    private bool _isOptimizedDuringBuild;
    public bool IsOptimizedDuringBuild
    {
        get => _isOptimizedDuringBuild;
        set
        {
            _isOptimizedDuringBuild = value;
            OnPropertyChanged();
        }
    }

    private BitmapSource? _imageThumbnail;
    [JsonIgnore]
    public BitmapSource? ImageThumbnail
    {
        get => _imageThumbnail;
        set
        {
            if (_imageThumbnail != value)
            {
                _imageThumbnail = value;
                OnPropertyChanged(nameof(ImageThumbnail));
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsPreviewDisabled => DisplayTextureData?.Data?.FullData == null || DisplayTextureData.Data.FullData.Length == 0;

    [JsonIgnore]
    public string PreviewDisabledTooltip => IsPreviewDisabled ? "Encrypted drawable" : string.Empty;

    // Parameterless constructor for JSON deserialization
    public GTextureEmbedded()
    {
        OriginalName = string.Empty;
        Details = new GTextureDetails();
    }

    public GTextureEmbedded(CodeWalker.GameFiles.Texture? textureData, string type)
    {
        TextureData = textureData;

        if (textureData == null)
        {
            OriginalName = "Missing texture";
            Details.Name = "Missing texture";
            Details.Type = type;
            Details.Width = 0;
            Details.Height = 0;
            Details.MipMapCount = 0;
            Details.Compression = "N/A";
        }
        else
        {
            OriginalName = textureData.Name;

            Details.Name = textureData.Name;
            Details.Type = type;
            Details.Width = textureData.Width;
            Details.Height = textureData.Height;
            Details.MipMapCount = textureData.Levels;
            Details.Compression = textureData.Format.ToString();
            
            Details.Validate();
            LoadThumbnailAsync();
        }
    }

    public void SetReplacementTexture(CodeWalker.GameFiles.Texture newTexture)
    {
        ReplacementTextureData = newTexture;
        
        Details.Name = newTexture.Name;
        Details.Width = newTexture.Width;
        Details.Height = newTexture.Height;
        Details.MipMapCount = newTexture.Levels;
        Details.Compression = newTexture.Format.ToString();
        Details.Validate();
        
        OnPropertyChanged(nameof(Details));
        
        ImageThumbnail = null;
        LoadThumbnailAsync();
    }

    public void RenameTexture(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || DisplayTextureData == null)
            return;

        Details.Name = newName;
        OnPropertyChanged(nameof(Details));
    }

    public async void LoadThumbnailAsync()
    {
        if (ImageThumbnail != null || DisplayTextureData?.Data?.FullData == null)
            return;

        IsLoading = true;

        await Task.Delay(Random.Shared.Next(25, 100));
        await Task.Run(() =>
        {
            try
            {
                if (DisplayTextureData.Data.FullData.Length == 0)
                    return;

                var dds = DDSIO.GetDDSFile(DisplayTextureData);
                using var img = new MagickImage(dds);
                
                img.Resize(90, 90);
                int w = (int)img.Width;
                int h = (int)img.Height;
                byte[] pixels = img.ToByteArray(MagickFormat.Bgra);

                using Bitmap bitmap = new(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, w, h),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var source = BitmapSource.Create(
                        bitmap.Width,
                        bitmap.Height,
                        96, 96,
                        PixelFormats.Bgra32,
                        null,
                        pixels,
                        bitmap.Width * 4
                    );
                    source.Freeze();
                    ImageThumbnail = source;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Embedded texture thumbnail generation failed: {ex.Message}");
                LogHelper.Log($"Could not generate embedded texture thumbnail for {Details.Name}");
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}