using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace grzyClothTool.Models;

public class GTextureDetails
{
    public string Format { get; set; }
    public int MipMapCount { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class GTexture : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    public FileInfo File;

    public string DisplayName
    {
        get { return GetName(HasSkin); }
    }

    public string InternalName { get; set; }
    public int Number;
    public int TxtNumber;
    public char TxtLetter;
    public int TypeNumeric { get; set; }
    public string TypeName => EnumHelper.GetName(TypeNumeric, IsProp);

    public bool IsProp;
    public bool HasSkin;

    public GTextureDetails Details { get; set; }
    public YtdFile Ytd { get; set; }

    public GTexture(string path, int compType, int drawableNumber, int txtNumber, bool hasSkin, bool isProp)
    {
        File = path.Length > 1 ? new FileInfo(path) : null;
        Number = drawableNumber;
        TxtNumber = txtNumber;
        TypeNumeric = compType;
        IsProp = isProp;
        HasSkin = hasSkin;

        // Ytd = CWHelper.GetYtdFile(path);
        // Details = CWHelper.GetTextureDetails(Ytd);
    }

    private string GetName(bool hasSkin)
    {
        TxtLetter = (char)('a' + TxtNumber);
        string name = $"{TypeName}_diff_{Number:D3}_{TxtLetter}";
        return IsProp ? name : $"{name}_{(hasSkin ? "whi" : "uni")}";
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public Texture GetCurrentTexture()
    {
        return Ytd.TextureDict.Textures[0];
    }
}
