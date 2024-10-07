using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using ImageMagick;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTexture : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);

    public event PropertyChangedEventHandler PropertyChanged;
    public string FilePath;
    public string Extension;

    public string DisplayName
    {
        get { return GetName(HasSkin); }
    }

    public int Number;
    private int _txtNumber;
    public int TxtNumber
    {
        get => _txtNumber;
        set
        {
            _txtNumber = value;
            OnPropertyChanged("DisplayName");
        }
    }
    public char TxtLetter;
    public int TypeNumeric { get; set; }
    public string TypeName => EnumHelper.GetName(TypeNumeric, IsProp);

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
    public bool IsProp;
    public bool HasSkin;

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

    public GTexture(string path, int compType, int drawableNumber, int txtNumber, bool hasSkin, bool isProp)
    {
        IsLoading = true;

        FilePath = path;
        Extension = Path.GetExtension(path);
        Number = drawableNumber;
        TxtNumber = txtNumber;
        TypeNumeric = compType;
        IsProp = isProp;
        HasSkin = hasSkin;

        if (path != null)
        {
            Task<GTextureDetails?> _textureDetailsTask = LoadTextureDetailsWithConcurrencyControl(path).ContinueWith(t =>
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


    private string GetName(bool hasSkin)
    {
        TxtLetter = (char)('a' + TxtNumber);
        string name = $"{TypeName}_diff_{Number:D3}_{TxtLetter}";
        return IsProp ? name : $"{name}_{(hasSkin ? "whi" : "uni")}";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                Name = txt.Name
            };
        }
        else if (extension == ".jpg" || extension == ".png" || extension == ".dds")
        {
            using var img = new MagickImage(bytes);

            return new GTextureDetails
            {
                Width = img.Width,
                Height = img.Height,
                MipMapCount = ImgHelper.GetCorrectMipMapAmount(img.Width, img.Height),
                Compression = "UNKNOWN",
                Name = img.FileName
            };
        }

        return null;

    }
}
