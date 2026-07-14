using grzyClothTool.Helpers;
using System;

namespace grzyClothTool.Constants;

public static class GlobalConstants
{
    public const int MAX_DRAWABLES_IN_ADDON_LIMIT = 256;

    public static int MAX_DRAWABLES_IN_ADDON => SettingsHelper.Instance.MaxDrawablesPerAddon;

    public const int MAX_DRAWABLE_TEXTURES = 26;
    public const string ASSETS_FOLDER_NAME = "project_assets";
    public static readonly Uri DISCORD_INVITE_URL = new("https://discord.gg/HCQutNhxWt");
    public static readonly string GRZY_TOOLS_URL = "https://grzy.tools";
}
