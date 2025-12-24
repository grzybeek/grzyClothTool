using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System;
using System.IO;

namespace grzyClothTool.Models.Drawable;

public class GDrawableReserved : GDrawable
{
    public GDrawableReserved(Enums.SexType sex, bool isProp, int compType, int count) : base(sex, isProp, compType, count)
    {
        FilePath = Path.Combine(FileHelper.ReservedAssetsPath, "reservedDrawable.ydd");
        Textures = [new GTexture(Guid.Empty, Path.Combine(FileHelper.ReservedAssetsPath, "reservedTexture.ytd"), compType, count, 0, false, isProp)];
        TypeNumeric = compType;
        Number = count;
        Sex = sex;
        IsProp = isProp;
        IsReserved = true;

        SetDrawableName();
    }
}
