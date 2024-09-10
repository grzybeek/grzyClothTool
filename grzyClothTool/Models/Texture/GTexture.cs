using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using ImageMagick;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTexture : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);
    private readonly static SemaphoreSlim _semaphoreDublicateCheck = new(1);

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

    [JsonIgnore]
    internal static Tuple<Dictionary<string, Dictionary<string, List<GTexture>>>, Dictionary<string, Dictionary<string, List<GTexture>>>> hashes = Tuple.Create(new Dictionary<string, Dictionary<string, List<GTexture>>>(), new Dictionary<string, Dictionary<string, List<GTexture>>>());

    [JsonIgnore]
    private bool _isDuplicate = false;

    [JsonIgnore]
    public bool IsDuplicate
    {
        get => SettingsHelper.Instance.DisplayHashDuplicate && _isDuplicate;
        set
        {
            _isDuplicate = value;
            OnPropertyChanged(nameof(IsDuplicate));
        }
    }

    [JsonIgnore]
    private List<GTexture> _isDuplicateName = [];

    [JsonIgnore]
    public string IsDuplicateName
    {
        get
        {
            if (IsDuplicate == false)
            {
                return "";
            }
            var namedDuplicateList = _isDuplicateName.Select(texture => {
                Addon? addon = MainWindow.AddonManager.Addons.FirstOrDefault(a => a?.Drawables?.Any(drawable => drawable.Textures.Contains(texture)) == true, null);
                if (addon == null)
                {
                    return "Not found: " + texture.DisplayName;
                }
                return addon.Name + "/" + texture.DisplayName;
            });
            return string.Join(", ", namedDuplicateList);
        }
    }

    [JsonIgnore]
    public List<GTexture> IsDuplicateNameSetter
    {
        set
        {
            _isDuplicateName = value;
            OnPropertyChanged(nameof(_isDuplicateName));
        }
    }

    public bool IsProp;
    public bool HasSkin;

    public bool IsOptimizedDuringBuild { get; set; }
    public GTextureDetails OptimizeDetails;

    public bool IsPreviewDisabled { get; set; }

    [JsonIgnore]
    public Task TextureDetailsTask { get; set; } = Task.CompletedTask;

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

        Load();
    }

    private void Load()
    {
        TextureDetailsTask = Task.Run(async () => {
            try
            {
                if (FilePath != null)
                {
                    var gTextureDetails = await LoadTextureDetailsWithConcurrencyControl(FilePath);
                    if (gTextureDetails == null)
                    {
                        IsPreviewDisabled = true;
                    }
                    else
                    {
                        TxtDetails = gTextureDetails;
                        OnPropertyChanged(nameof(TxtDetails));

                        TxtDetails.Validate();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //todo: add some warning that it couldn't load
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (IsLoading)
        {
            Load();
        }
        else
        {
            TextureDetailsTask = Task.CompletedTask;
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
        string hash = string.Concat(MD5.HashData(bytes).Select(x => x.ToString("X2")));

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
                Hash = hash
            };
        }
        else if (extension == ".jpg" || extension == ".png")
        {
            using var img = new MagickImage(bytes);

            return new GTextureDetails
            {
                Width = img.Width,
                Height = img.Height,
                MipMapCount = ImgHelper.GetCorrectMipMapAmount(img.Width, img.Height),
                Compression = "UNKNOWN",
                Name = img.FileName,
                Hash = hash
            };
        }

        return null;

    }

    public async Task CheckForDuplicate(string drawableHash, bool isMale)
    {
        if (!SettingsHelper.Instance.DisplayHashDuplicate) return;
        await TextureDetailsTask;
        if (TxtDetails == null) return;
        await _semaphoreDublicateCheck.WaitAsync();
        List<Task> promises = [];
        try
        {
            IsDuplicate = false;
            if (drawableHash != "" && TxtDetails.Hash != "")
            {
                var sexHashes = isMale ? hashes.Item1 : hashes.Item2;
                if (sexHashes.TryGetValue(drawableHash, out Dictionary<string, List<GTexture>>? group))
                {
                    if (group.TryGetValue(TxtDetails.Hash, out List<GTexture>? duplicates))
                    {
                        if (!duplicates.Contains(this))
                        {
                            duplicates.Add(this);
                            foreach (GTexture texture in duplicates)
                            {
                                if (texture == this) continue;
                                promises.Add(texture.CheckForDuplicate(drawableHash, isMale));
                            }
                        }
                        if (duplicates.Any(h => h != this))
                        {
                            IsDuplicate = true;
                            IsDuplicateNameSetter = duplicates.FindAll(dup => dup != this);
                        }
                    }
                    else
                    {
                        group.Add(TxtDetails.Hash, [this]);
                    }
                }
                else
                {
                    sexHashes.Add(drawableHash, []);
                    sexHashes[drawableHash].Add(TxtDetails.Hash, [this]);
                }
            }
        }
        finally
        {
            _semaphoreDublicateCheck.Release();
            await Task.WhenAll(promises);
        }
    }

    public async Task RemoveDuplicate(string drawableHash, bool isMale)
    {
        if (!SettingsHelper.Instance.DisplayHashDuplicate) return;
        await TextureDetailsTask;
        if (TxtDetails == null) return;
        await _semaphoreDublicateCheck.WaitAsync();
        List<Task> promises = [];
        try
        {
            var sexHashes = isMale ? hashes.Item1 : hashes.Item2;
            if (sexHashes.TryGetValue(drawableHash, out Dictionary<string, List<GTexture>>? group) && group.TryGetValue(TxtDetails.Hash, out List<GTexture>? duplicates))
            {
                duplicates.Remove(this);
                var oldList = duplicates;

                // refresh duplicates
                group.Remove(TxtDetails.Hash);
                foreach (GTexture duplicate in oldList)
                {
                    promises.Add(duplicate.CheckForDuplicate(drawableHash, isMale));
                }
            }
        }
        finally
        {
            _semaphoreDublicateCheck.Release();
            await Task.WhenAll(promises);
        }
    }
}
