using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System.IO;

namespace grzyClothTool.Models.Drawable;

public class GDrawableReserved : GDrawable
{
    public override bool IsReserved => true;

    public GDrawableReserved(bool isMale, bool isProp, int compType, int count) : base(isMale, isProp, compType, count)
    {
        FilePath = Path.Combine(FileHelper.ReservedAssetsPath, "reservedDrawable.ydd");
        Textures = [new GTexture(Path.Combine(FileHelper.ReservedAssetsPath, "reservedTexture.ytd"), compType, count, 0, false, isProp)];
        TypeNumeric = compType;
        Number = count;
        Sex = isMale;
        IsProp = isProp;

        SetDrawableName();
    }
}
