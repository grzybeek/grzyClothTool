using grzyClothTool.Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using CodeWalker.GameFiles;
using System.Linq;
using System;
using grzyClothTool.Controls;
using System.Runtime.Serialization;
using grzyClothTool.Models.Texture;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawable : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);

    public event PropertyChangedEventHandler PropertyChanged;

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

    public GDrawableDetails Details { get; set; }

    public string? FirstPersonPath { get; set; } = null;
    public string? ClothPhysicsPath { get; set; } = null;


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

    [JsonIgnore]
    public List<SelectableItem> AvailableFlags => EnumHelper.GetFlags(Flags);

    public string RenderFlag { get; set; } = ""; // "" is the default value

    [JsonIgnore]
    public static List<string> AvailableRenderFlagList => ["", "PRF_ALPHA", "PRF_DECAL", "PRF_CUTOUT"];

    public ObservableCollection<Texture.GTexture> Textures { get; set; }

    public GDrawable(string drawablePath, Enums.SexType sex, bool isProp, int compType, int count, bool hasSkin, ObservableCollection<GTexture> textures)
    {
        IsLoading = true;

        FilePath = drawablePath;
        Textures = textures;
        TypeNumeric = compType;
        Number = count;
        HasSkin = hasSkin;
        Sex = sex;
        IsProp = isProp;
        IsNew = true;

        Audio = "none";
        SetDrawableName();

        if (FilePath != null)
        {
            Task<GDrawableDetails?> _drawableDetailsTask = LoadDrawableDetailsWithConcurrencyControl(FilePath).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Console.WriteLine(t.Exception);
                    //todo: add some warning that it couldn't load
                    IsLoading = true;
                    return null;
                }

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    if (t.Result == null)
                    {
                        return null;
                    }

                    Details = t.Result;
                    OnPropertyChanged(nameof(Details));
                    IsLoading = false;
                }

                return t.Result;
            });
        }
    }


    //this will be called after deserialization
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
       
        SetDrawableName();
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
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static async Task<GDrawableDetails?> LoadDrawableDetailsWithConcurrencyControl(string path)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await GetDrawableDetailsAsync(path);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<GDrawableDetails?> GetDrawableDetailsAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);

        var yddFile = new YddFile();
        await yddFile.LoadAsync(bytes);

        if (yddFile.DrawableDict.Drawables.Count == 0)
        {
            return null;
        }

        GDrawableDetails details = new();


        //is it always 2 and 3?
        var spec = (yddFile.Drawables.First().ShaderGroup.Shaders.data_items.First().ParametersList.Parameters[3].Data as CodeWalker.GameFiles.Texture);
        var normal = (yddFile.Drawables.First().ShaderGroup.Shaders.data_items.First().ParametersList.Parameters[2].Data as CodeWalker.GameFiles.Texture);

        foreach (GDrawableDetails.EmbeddedTextureType txtType in Enum.GetValues(typeof(GDrawableDetails.EmbeddedTextureType)))
        {
            var texture = txtType switch
            {
                GDrawableDetails.EmbeddedTextureType.Specular => spec,
                GDrawableDetails.EmbeddedTextureType.Normal => normal,
                _ => null
            };

            if (texture == null)
            {
                continue;
            }

            details.EmbeddedTextures[txtType] = new GTextureDetails
            {
                Width = texture.Width,
                Height = texture.Height,
                Name = texture.Name,
                Type = txtType.ToString(),
                MipMapCount = texture.Levels,
                Compression = texture.Format.ToString()
            };
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

        details.Validate();
        return details;
    }
}
