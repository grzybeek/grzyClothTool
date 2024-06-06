using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Views;
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
using grzyClothTool.Models.Texture;
using System;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawable : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);

    public event PropertyChangedEventHandler PropertyChanged;

    public string FilePath { get; set; }

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

    [JsonIgnore]
    public List<string> AvailableTypes => IsProp ? EnumHelper.GetPropTypeList() : EnumHelper.GetDrawableTypeList();

    /// <returns>
    /// true(1) = male ped, false(0) = female ped
    /// </returns>
    public bool Sex { get; set; }
    public bool IsProp { get; set; }
    public bool IsComponent => !IsProp;

    public int Number { get; set; }
    public string DisplayNumber => (Number % GlobalConstants.MAX_DRAWABLES_IN_ADDON).ToString("D3");

    public GDrawableDetails Details { get; set; }


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

    public string RenderFlag { get; set; } = ""; // "" is the default value

    [JsonIgnore]
    public static List<string> AvailableRenderFlagList => ["", "PRF_ALPHA", "PRF_DECAL", "PRF_CUTOUT"];

    public ObservableCollection<GTexture> Textures { get; set; }

    public GDrawable(string drawablePath, bool isMale, bool isProp, int compType, int count, bool hasSkin, ObservableCollection<GTexture> textures)
    {
        IsLoading = true;

        FilePath = drawablePath;
        Textures = textures;
        TypeNumeric = compType;
        Number = count;
        HasSkin = hasSkin;
        Sex = isMale;
        IsProp = isProp;

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

    protected GDrawable(bool isMale, bool isProp, int compType, int count) { /* Used in GReservedDrawable */ }

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

        // change current drawable to new type
        Number = MainWindow.AddonManager.SelectedAddon.GetNextDrawableNumber(newTypeNumeric, IsProp, Sex);
        TypeNumeric = newTypeNumeric;
        SetDrawableName();

        // add new drawable with new number and type
        MainWindow.AddonManager.SelectedAddon.Drawables.Insert(index + 1, this);

        // replace drawable with reserved in the same place
        MainWindow.AddonManager.SelectedAddon.Drawables[index] = reserved;

        MainWindow.AddonManager.SelectedAddon.Drawables.Sort();
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

        GDrawableDetails details = new()
        {
            EmbeddedTextures = yddFile.Drawables.First().ShaderGroup.TextureDictionary?.Textures?.Select(t =>
            {
                var textureDetails = new GTextureDetails
                {
                    Width = t.Width,
                    Height = t.Height,
                    Name = t.Name,
                    MipMapCount = t.Levels,
                    Compression = t.Format.ToString()
                };

                textureDetails.Validate();
                return textureDetails;
            }).ToList() ?? []
        };

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

        return details;
    }
}
