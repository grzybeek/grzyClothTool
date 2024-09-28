using grzyClothTool.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using static grzyClothTool.Enums;

namespace grzyClothTool.Helpers;
public static class EnumHelper
{
    public static string GetName(int type, bool isProp)
    {
        Type enumType = isProp ? typeof(Enums.PropNumbers) : typeof(Enums.ComponentNumbers);
        return Enum.GetName(enumType, type);
    }

    public static int GetValue(string type, bool isProp)
    {
        Type enumType = isProp ? typeof(Enums.PropNumbers) : typeof(Enums.ComponentNumbers);
        return (int)Enum.Parse(enumType, type);
    }

    public static List<string> GetAudioList(int typeNumeric)
    {
        ComponentNumbers component = (ComponentNumbers)typeNumeric;
        return component switch
        {
            ComponentNumbers.berd => ["none", "cloth_scuba"],
            ComponentNumbers.uppr => [
                "none",
                "cloth_default",
                "cloth_upper_bare",
                "cloth_upper_sweater",
                "cloth_upper_shirt_tee",
                "cloth_upper_shirt_cotton_lite",
                "cloth_upper_shirt_cotton_heavy",
                "cloth_upper_jacket_cotton",
                "cloth_upper_jacket_puffy",
                "cloth_upper_jacket_leather",
                "cloth_upper_jacket_suit",
                "cloth_upper_bikini_top",
                "cloth_upper_spacesuit",
                "cloth_upper_alien",
                "cloth_upper_coat_scientist",
                "cloth_upper_cop_vest_helmet"
            ],
            ComponentNumbers.lowr => [
                "none",
                "cloth_default",
                "cloth_lower_cotton",
                "cloth_lower_leather",
                "cloth_lower_shorts",
                "cloth_lower_pants_leather",
                "cloth_lower_pants_denim",
                "cloth_lower_pants_suit",
                "cloth_lower_pants_tight",
                "cloth_lower_skirt_short",
                "cloth_lower_skirt_long",
                "cloth_lower_waterproof",
                "cloth_lower_bare",
                "cloth_lower_ballistic_armour",
                "cloth_lower_extreme",
                "cloth_lower_swat",
                "cloth_lower_fireman",
                "cloth_lower_fireman_lower"
            ],
            ComponentNumbers.hand => ["none", "cloth_rappel_parachute", "cloth_heavy_bag"],
            ComponentNumbers.feet => [
                "none",
                "footsteps_generic",
                "shoe_barefoot",
                "shoe_heels",
                "shoe_normal_heels",
                "shoe_high_heels",
                "shoe_heavy_boots",
                "shoe_rubber_boots",
                "shoe_rubber",
                "shoe_flip_flops",
                "shoe_dress_shoes",
                "shoe_trainers",
                "shoe_scuba_flippers",
                "shoe_cowboy_boots",
                "shoe_clown_shoes",
                "shoe_gold_shoes",
                "shoe_silent"
            ],
            ComponentNumbers.accs => [
                "none",
                "cloth_default",
                "cloth_upper_shirt_tee",
                "cloth_upper_cotton",
                "cloth_upper_shirt_cotton_lite",
                "cloth_upper_jacket_puffy",
                "cloth_upper_jacket_suit",
                "cloth_upper_ballistic_armour",
                "cloth_upper_bikini_top",
                "cloth_upper_cop_vest",
                "cloth_scuba",
                "cloth_cop_belt",
                "cloth_gas_mask"
            ],
            ComponentNumbers.task => [
                "none",
                "cloth_ballistic",
                "cloth_backpack",
                "cloth_tool_belt"
            ],
            ComponentNumbers.jbib => [
                "none",
                "cloth_default",
                "cloth_upper_bare",
                "cloth_upper_leather",
                "cloth_upper_sweater",
                "cloth_upper_cotton",
                "cloth_upper_shirt_tee",
                "cloth_upper_shirt_cotton_lite",
                "cloth_upper_shirt_cotton_heavy",
                "cloth_upper_shirt_leather",
                "cloth_upper_jacket_cotton",
                "cloth_upper_jacket_puffy",
                "cloth_upper_jacket_leather",
                "cloth_upper_jacket_suit",
                "cloth_upper_bikini_top",
                "cloth_upper_waterproof",
                "cloth_upper_ballistic_armour",
                "cloth_upper_spacesuit",
                "cloth_upper_swat",
                "cloth_upper_fireman",
                "cloth_upper_kifflom",
                "cloth_upper_alien"
            ],
            //head, hair, teef, decl
            _ => ["none"],
        };
    }

    public static List<SelectableItem> GetFlags(int selected = 0)
    {
        var flags = Enum.GetValues(typeof(DrawableFlags))
                        .Cast<DrawableFlags>()
                        .Where(flag => flag != DrawableFlags.NONE) // Exclude NONE flag
                        .Select(flag =>
                            new SelectableItem(
                                flag.ToString(),
                                (int)flag,
                                (selected & (int)flag) == (int)flag
                            )
                        ).ToList();
        return flags;
    }

    public static List<string> GetDrawableTypeList()
    {
        return Enum.GetValues(typeof(ComponentNumbers))
            .Cast<ComponentNumbers>()
            .Select(item => item.ToString())
            .ToList();
    }

    public static List<string> GetPropTypeList()
    {
        return Enum.GetValues(typeof(PropNumbers))
            .Cast<PropNumbers>()
            .Select(item => item.ToString())
            .ToList();
    }

    public static List<string> GetSexTypeList()
    {
        return Enum.GetValues(typeof(SexType))
            .Cast<SexType>()
            .Select(item => item.ToString())
            .ToList();
    }
}
