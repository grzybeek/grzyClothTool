using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace grzyClothTool.Models;

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

    public bool IsProp;
    public bool HasSkin;

    public GTexture(string path, int compType, int drawableNumber, int txtNumber, bool hasSkin, bool isProp)
    {
        File = path.Length > 1 ? new FileInfo(path) : null;
        Number = drawableNumber;
        TxtNumber = txtNumber;
        TypeNumeric = compType;
        IsProp = isProp;
        HasSkin = hasSkin;
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
}
