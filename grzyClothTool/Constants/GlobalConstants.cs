using System;

namespace grzyClothTool.Constants;

public static class GlobalConstants
{
    public const int MAX_DRAWABLES_IN_ADDON = 128; //todo: somewhere in the future, this should depend on a resource type (fivem has limit of 128, but sp and ragemp have 255 I believe)
    public const int MAX_DRAWABLE_TEXTURES = 26;
    public const long MAX_RESOURCE_SIZE_BYTES = 838860800; // 800 MB in bytes
    public const long MAX_MAIN_RESOURCE_SIZE_BYTES = 734003200; // 700 MB in bytes (800 * 1024 * 1024)
    public static readonly Uri DISCORD_INVITE_URL = new("https://discord.gg/HCQutNhxWt");
    public static readonly string GRZY_TOOLS_URL = "https://grzy.tools";
}
