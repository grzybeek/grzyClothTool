using System;

namespace grzyClothTool;

public static class GlobalConstants
{
    public const int MAX_DRAWABLES_IN_ADDON = 128; //todo: somewhere in the future, this should depend on a resource type (fivem has limit of 128, but sp and ragemp have 255 I believe)
    public static readonly Uri DISCORD_INVITE_URL = new("https://discord.gg/HCQutNhxWt");
}
