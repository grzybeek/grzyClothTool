using grzyClothTool.Constants;
using System.Collections.Generic;
using static grzyClothTool.Enums;

namespace grzyClothTool.Helpers;

public static class PedAlternativeVariationsHelper
{

    public static List<DlcEntry> GetHairEntriesForSex(SexType sex)
    {
        return sex == SexType.male 
            ? PedAlternateVariationsConstants.MaleHairs 
            : PedAlternateVariationsConstants.FemaleHairs;
    }

    public static List<DlcEntry> GetMaskEntriesForSex(SexType sex)
    {
        return sex == SexType.male 
            ? PedAlternateVariationsConstants.MaleMasks 
            : PedAlternateVariationsConstants.FemaleMasks;
    }
}
