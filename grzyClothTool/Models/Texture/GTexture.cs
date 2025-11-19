using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using ImageMagick;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Text.Json.Serialization;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTexture : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);

    public event PropertyChangedEventHandler PropertyChanged;

    public Guid Id { get; set; }
    public string FilePath { get; set; }
    public string Extension { get; set; }

    private string _displayName = string.Empty;
    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    private int _number;
    public int Number
    {
        get => _number;
        set
        {
            _number = value;
            OnPropertyChanged(nameof(Number));
            UpdateDisplayName();
        }
    }

    private int _txtNumber;
    public int TxtNumber
    {
        get => _txtNumber;
        set
        {
            _txtNumber = value;
            
            OnPropertyChanged();
            OnPropertyChanged("BuildName");
            OnPropertyChanged("TxtLetter");
            UpdateDisplayName();
        }
    }

    public char TxtLetter
    {
        get => (char)('a' + TxtNumber);
    }

    public int TypeNumeric { get; set; }
    public string TypeName => EnumHelper.GetName(TypeNumeric, IsProp);

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

    public GTextureDetails TxtDetails { get; set; }

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
    public bool IsProp { get; set; }
    public bool HasSkin { get; set; }

    private bool _isOptimizedDuringBuild;
    public bool IsOptimizedDuringBuild
    {
        get => _isOptimizedDuringBuild;
        set
        {
            if (_isOptimizedDuringBuild != value)
            {
                _isOptimizedDuringBuild = value;
                OnPropertyChanged(nameof(IsOptimizedDuringBuild));
            }
        }
    }
    private GTextureDetails _optimizeDetails;
    public GTextureDetails OptimizeDetails
    {
        get => _optimizeDetails;
        set
        {
            if (_optimizeDetails != value)
            {
                _optimizeDetails = value;
                OnPropertyChanged(nameof(OptimizeDetails));
            }
        }
    }

    public bool IsPreviewDisabled { get; set; }

    public GTexture(Guid id, string filePath, int typeNumeric, int number, int txtNumber, bool hasSkin, bool isProp)
    {
        IsLoading = true;

        Id = id;
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        FilePath = filePath;
        Extension = Path.GetExtension(filePath);
        Number = number;
        TxtNumber = txtNumber;
        TypeNumeric = typeNumeric;
        IsProp = isProp;
        HasSkin = hasSkin;
        DisplayName = GetBuildName();

        if (filePath != null)
        {
            Task<GTextureDetails?> _textureDetailsTask = LoadTextureDetailsWithConcurrencyControl(filePath).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine(t.Exception);
                    //todo: add some warning that it couldn't load
                    IsLoading = true;
                    return null;
                }

                IsLoading = false; // Loading finished
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    if (t.Result == null)
                    {
                        IsPreviewDisabled = true;
                        return null;
                    }

                    TxtDetails = t.Result;
                    OnPropertyChanged(nameof(TxtDetails));

                    TxtDetails.Validate();
                }

                return t.Result;
            });
        }
    }

    public async void LoadThumbnailAsync()
    {
        if (ImageThumbnail != null)
            return;

        if (FilePath == null || !File.Exists(FilePath))
            return;

        await Task.Delay(Random.Shared.Next(25, 100));
        await Task.Run(() =>
        {
            try
            {
                using MagickImage img = ImgHelper.GetImage(FilePath);
                if (img == null)
                    return;

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
                Console.WriteLine($"Image thumbnail generation failed: {ex.Message}");
                LogHelper.Log($"Could not generate image thumbnail for {DisplayName}");
            }
        });
    }



    public string GetBuildName()
    {
        string name = $"{TypeName}_diff_{Number:D3}_{TxtLetter}";
        return IsProp ? name : $"{name}_{(HasSkin ? "whi" : "uni")}";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public async Task LoadDetails()
    {
        if (File.Exists(FilePath))
        {
            var result = await LoadTextureDetailsWithConcurrencyControl(FilePath);
            if (result != null)
            {
                TxtDetails = result;
                OnPropertyChanged(nameof(TxtDetails));
                TxtDetails.Validate();
            }
        }

        IsLoading = false;
    }

    private void UpdateDisplayName()
    {
        DisplayName = GetBuildName();
    }

    private static async Task<GTextureDetails?> LoadTextureDetailsWithConcurrencyControl(string path)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await GetTextureDetailsAsync(path);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private static async Task<GTextureDetails?> GetTextureDetailsAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var extension = Path.GetExtension(path);

        if (extension == ".ytd")
        {
            var ytdFile = new YtdFile();
            await ytdFile.LoadAsync(bytes);

            if (ytdFile.TextureDict.Textures.Count == 0)
            {
                return null;
            }

            var txt = ytdFile.TextureDict.Textures[0];

            return new GTextureDetails
            {
                MipMapCount = txt.Levels,
                Compression = txt.Format.ToString(),
                Width = txt.Width,
                Height = txt.Height,
                Name = txt.Name,
                Type = "diffuse"
            };
        }
        else if (extension == ".jpg" || extension == ".png" || extension == ".dds")
        {
            using var img = new MagickImage(bytes);

            return new GTextureDetails
            {
                Width = (int)img.Width,
                Height = (int)img.Height,
                MipMapCount = ImgHelper.GetCorrectMipMapAmount((int)img.Width, (int)img.Height),
                Compression = "UNKNOWN",
                Name = img.FileName,
                Type = "diffuse"
            };
        }

        return null;
    }
}

