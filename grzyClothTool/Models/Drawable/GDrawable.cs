using CodeWalker.GameFiles;
using grzyClothTool.Constants;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawable : INotifyPropertyChanged
{
    private static readonly BlockingCollection<GDrawable> _loadQueue = new();
    private readonly static SemaphoreSlim _semaphore = new(3);

    static GDrawable()
    {
        // Start a few consumer tasks to process the queue
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            Task.Run(ProcessLoadQueue);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public Guid Id { get; set; }

    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isNew;
    public bool IsNew
    {
        get => _isNew;
        set
        {
            if (_isNew != value)
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public virtual bool IsReserved => false;

    private bool _isEncrypted;
    public bool IsEncrypted
    {
        get => _isEncrypted;
        set
        {
            if (_isEncrypted != value)
            {
                _isEncrypted = value;
                OnPropertyChanged();
            }
        }
    }

    public int TypeNumeric { get; set; }
    private string _typeName;
    public string TypeName
    {
        get
        {
            _typeName ??= EnumHelper.GetName(TypeNumeric, IsProp);
            return _typeName;
        }
        set
        {
            _typeName = value;

            //TypeNumeric = EnumHelper.GetValue(value, IsProp);

            SetDrawableName();
            OnPropertyChanged();
        }
    }

    private string _sexName;
    public string SexName
    {
        get
        {
            return _sexName ??= Enum.GetName(Sex)!;
        }
        set
        {
            _sexName = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public List<string> AvailableTypes => IsProp ? EnumHelper.GetPropTypeList() : EnumHelper.GetDrawableTypeList();

    [JsonIgnore]
    public static List<string> AvailableSex => EnumHelper.GetSexTypeList();

    private Enums.SexType _sex;
    public Enums.SexType Sex
    {
        get => _sex;
        set
        {
            _sex = value;
            OnPropertyChanged();
        }
    }

    public bool IsProp { get; set; }
    public bool IsComponent => !IsProp;

    public int Number { get; set; }
    public string DisplayNumber => (Number % GlobalConstants.MAX_DRAWABLES_IN_ADDON).ToString("D3");

    private GDrawableDetails _details;
    public GDrawableDetails Details
    {
        get => _details;
        set
        {
            if (_details != value)
            {
                _details = value;
                OnPropertyChanged();
            }
        }
    }

    public string? FirstPersonPath { get; set; } = null;
    public string? ClothPhysicsPath { get; set; } = null;

    private string? _group;
    public string? Group
    {
        get => _group;
        set
        {
            if (_group != value)
            {
                _group = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _hasSkin;
    public bool HasSkin
    {
        get { return _hasSkin; }
        set
        {
            if (_hasSkin != value)
            {
                _hasSkin = value;

                foreach (var txt in Textures)
                {
                    txt.HasSkin = value;
                }
                SetDrawableName();
                OnPropertyChanged();
            }
        }
    }

    private bool _enableKeepPreview;
    public bool EnableKeepPreview
    {
        get => _enableKeepPreview;
        set { _enableKeepPreview = value; OnPropertyChanged(); }
    }

    public float HairScaleValue { get; set; } = 0.5f;


    private bool _enableHairScale;
    public bool EnableHairScale
    {
        get => _enableHairScale;
        set { _enableHairScale = value; OnPropertyChanged(); }
    }

    public float HighHeelsValue { get; set; } = 1.0f;
    private bool _enableHighHeels;
    public bool EnableHighHeels
    {
        get => _enableHighHeels;
        set { _enableHighHeels = value; OnPropertyChanged(); }
    }

    private bool _hidesHair;
    public bool HidesHair
    {
        get => _hidesHair;
        set
        {
            if (_hidesHair != value)
            {
                _hidesHair = value;
                OnPropertyChanged();
            }
        }
    }

    private string _audio;
    public string Audio
    {
        get => _audio;
        set
        {
            _audio = value;
            OnPropertyChanged();
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

    [JsonIgnore]
    public List<string> AvailableAudioList => EnumHelper.GetAudioList(TypeNumeric);

    private ObservableCollection<SelectableItem> _selectedFlags = [];
    public ObservableCollection<SelectableItem> SelectedFlags
    {
        get => _selectedFlags;
        set
        {
            _selectedFlags = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Flags));
            OnPropertyChanged(nameof(FlagsText));
        }
    }

    [JsonIgnore]
    public string FlagsText
    {
        get
        {
            var count = SelectedFlags.Count(i => i.IsSelected && i.Value != (int)Enums.DrawableFlags.NONE);

            return count > 0 ? $"{Flags} ({count} selected)" : "NONE";
        } 
    }

    [JsonIgnore]
    public int Flags => SelectedFlags.Where(f => f.IsSelected).Sum(f => f.Value);

    private List<SelectableItem> _availableFlags;

    [JsonIgnore]
    public List<SelectableItem> AvailableFlags => _availableFlags;

    public string RenderFlag { get; set; } = ""; // "" is the default value

    [JsonIgnore]
    public static List<string> AvailableRenderFlagList => ["", "PRF_ALPHA", "PRF_DECAL", "PRF_CUTOUT"];

    public ObservableCollection<Texture.GTexture> Textures { get; set; }

    public GDrawable(Guid id, string filePath, Enums.SexType sex, bool isProp, int typeNumeric, int number, bool hasSkin, ObservableCollection<GTexture> textures)
    {
        IsLoading = true;

        Id = id;
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        FilePath = filePath;
        Textures = textures;
        Textures.CollectionChanged += OnTexturesCollectionChanged;
        TypeNumeric = typeNumeric;
        Number = number;
        HasSkin = hasSkin;
        Sex = sex;
        IsProp = isProp;
        IsNew = true;

        _availableFlags = EnumHelper.GetFlags(Flags);
        Audio = "none";
        SetDrawableName();

        if (FilePath != null && !IsEncrypted && File.Exists(FilePath))
        {
            _loadQueue.Add(this);
        }
        else
        {
            IsLoading = false;
        }
    }

    public async Task LoadDetails()
    {
        if (File.Exists(FilePath))
        {
            var result = await LoadDrawableDetailsWithConcurrencyControl();
            if (result != null)
            {
                Details = result;
                OnPropertyChanged(nameof(Details));
            }
        }

        IsLoading = false;
    }

    private static async void ProcessLoadQueue()
    {
        foreach (var drawable in _loadQueue.GetConsumingEnumerable())
        {
            await drawable.LoadDetails();
        }
    }

    public void LoadTexturesThumbnail()
    {
        foreach (var texture in Textures)
        {
            texture.LoadThumbnailAsync();
        }
    }

    protected GDrawable(Enums.SexType sex, bool isProp, int compType, int count) { /* Used in GReservedDrawable */ }

    public void SetDrawableName()
    {
        string name = $"{TypeName}_{DisplayNumber}";
        var finalName = IsProp ? name : $"{name}_{(HasSkin ? "r" : "u")}";

        Name = finalName;
        //texture number needs to be updated too
        foreach (var txt in Textures)
        {
            txt.Number = Number;
            txt.TypeNumeric = TypeNumeric;
        }

        OnPropertyChanged(nameof(Name));
    }

    public void ChangeDrawableType(string newType)
    {
        var newTypeNumeric = EnumHelper.GetValue(newType, IsProp);
        var reserved = new GDrawableReserved(Sex, IsProp, TypeNumeric, Number);
        var index = MainWindow.AddonManager.SelectedAddon.Drawables.IndexOf(this);

        // change drawable to new type
        TypeNumeric = newTypeNumeric;

        // replace drawable with reserved in the same place
        MainWindow.AddonManager.SelectedAddon.Drawables[index] = reserved;

        // re-add changed drawable
        MainWindow.AddonManager.AddDrawable(this);
        MainWindow.AddonManager.Addons.Sort(true);
    }

    public void ChangeDrawableSex(string newSex)
    {
        // transform new sex to enum
        var newSexEnum = Enum.Parse<Enums.SexType>(newSex);
        var reserved = new GDrawableReserved(Sex, IsProp, TypeNumeric, Number);
        var index = MainWindow.AddonManager.SelectedAddon.Drawables.IndexOf(this);
    
        // change drawable sex
        Sex = newSexEnum;

        // replace drawable with reserved in the same place
        MainWindow.AddonManager.SelectedAddon.Drawables[index] = reserved;

        // re-add changed drawable
        MainWindow.AddonManager.AddDrawable(this);
        MainWindow.AddonManager.Addons.Sort(true);
    }

    private void OnTexturesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Details == null)
        {
            return;
        }

        Details.TexturesCount = Textures.Count;
        Details.Validate();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async Task<GDrawableDetails?> LoadDrawableDetailsWithConcurrencyControl()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await GetDrawableDetailsAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<GDrawableDetails?> GetDrawableDetailsAsync()
    {
        if (IsEncrypted)
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(FilePath);

        var yddFile = new YddFile();
        await yddFile.LoadAsync(bytes);

        if (yddFile.DrawableDict.Drawables.Count == 0)
        {
            return null;
        }

        GDrawableDetails details = new();

        var shaderGroup = yddFile.Drawables.First().ShaderGroup;
        var txtDict = shaderGroup.TextureDictionary;

        var uniqueSpecTextures = new HashSet<string>();
        var uniqueNormalTextures = new HashSet<string>();

        CodeWalker.GameFiles.Texture? spec = null;
        CodeWalker.GameFiles.Texture? normal = null;

        foreach (var shader in shaderGroup.Shaders.data_items)
        {
            var parameters = shader.ParametersList.Parameters;
            var hashes = shader.ParametersList.Hashes;

            for (int i = 0; i < hashes.Length && i < parameters.Length; i++)
            {
                var samplerName = hashes[i].ToString();

                if (samplerName.Equals("specsampler", StringComparison.OrdinalIgnoreCase))
                {
                    CodeWalker.GameFiles.Texture? foundSpec = null;

                    if (parameters[i].Data is CodeWalker.GameFiles.Texture embeddedTex)
                    {
                        foundSpec = embeddedTex;
                    }
                    else if (parameters[i].Data is TextureBase tb && txtDict != null)
                    {
                        foundSpec = txtDict.Lookup(tb.NameHash);
                    }

                    if (foundSpec != null && !uniqueSpecTextures.Contains(foundSpec.Name))
                    {
                        uniqueSpecTextures.Add(foundSpec.Name);
                        spec ??= foundSpec;
                    }
                }
                else if (samplerName.Equals("bumpsampler", StringComparison.OrdinalIgnoreCase))
                {
                    CodeWalker.GameFiles.Texture? foundNormal = null;

                    if (parameters[i].Data is CodeWalker.GameFiles.Texture embeddedTex)
                    {
                        foundNormal = embeddedTex;
                    }
                    else if (parameters[i].Data is TextureBase tb && txtDict != null)
                    {
                        foundNormal = txtDict.Lookup(tb.NameHash);
                    }

                    if (foundNormal != null && !uniqueNormalTextures.Contains(foundNormal.Name))
                    {
                        uniqueNormalTextures.Add(foundNormal.Name);
                        normal ??= foundNormal;
                    }
                }
            }
        }

        foreach (GDrawableDetails.EmbeddedTextureType txtType in Enum.GetValues(typeof(GDrawableDetails.EmbeddedTextureType)))
        {
            var texture = txtType switch
            {
                GDrawableDetails.EmbeddedTextureType.Specular => spec,
                GDrawableDetails.EmbeddedTextureType.Normal => normal,
                _ => null
            };

            details.EmbeddedTextures[txtType] = new GTextureEmbedded(texture, txtType.ToString());
        }

        var drawableModels = yddFile.Drawables.First().DrawableModels;
        foreach (GDrawableDetails.DetailLevel detailLevel in Enum.GetValues(typeof(GDrawableDetails.DetailLevel)))
        {
            var model = detailLevel switch
            {
                GDrawableDetails.DetailLevel.High => drawableModels.High,
                GDrawableDetails.DetailLevel.Med => drawableModels.Med,
                GDrawableDetails.DetailLevel.Low => drawableModels.Low,
                _ => null
            };

            if (model != null)
            {
                details.AllModels[detailLevel] = new GDrawableModel
                {
                    PolyCount = (int)model.Sum(y => y.Geometries.Sum(g => g.IndicesCount / 3))
                };
            }
        }

        details.TexturesCount = Textures.Count;

        details.Validate(Textures);
        return details;
    }
}
