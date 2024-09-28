namespace grzyClothTool;

public class Enums
{
    public enum SexType
    {
        female = 0,
        male = 1
    }

    public enum BuildResourceType
    {
        FiveM,
        AltV,
        Singleplayer
    }

    public enum ComponentNumbers
    {
        head = 0,
        berd = 1,
        hair = 2,
        uppr = 3,
        lowr = 4,
        hand = 5,
        feet = 6,
        teef = 7,
        accs = 8,
        task = 9,
        decl = 10,
        jbib = 11
    }

    public enum PropNumbers
    {
        p_head = 0,
        p_eyes = 1,
        p_ears = 2,
        p_mouth = 3,
        p_lhand = 4,
        p_rhand = 5,
        p_lwrist = 6,
        p_rwrist = 7,
        p_hip = 8,
        p_lfoot = 9,
        p_rfoot = 10,
        p_ph_l_hand = 11,
        p_ph_r_hand = 12
    }

    // https://alexguirre.github.io/rage-parser-dumps/dump.html?game=gta5&build=3179#ePedCompFlags
    public enum DrawableFlags
    {
        NONE = 0,
        BULKY = 1,
        JOB = 2,
        SUNNY = 4,
        WET = 8,
        COLD = 16,
        NOT_IN_CAR = 32,
        BIKE_ONLY = 64,
        NOT_INDOORS = 128,
        FIRE_RETARDENT = 256,
        ARMOURED = 512,
        LIGHTLY_ARMOURED = 1024,
        HIGH_DETAIL = 2048,
        DEFAULT_HELMET = 4096,
        RANDOM_HELMET = 8192,
        SCRIPT_HELMET = 16384,
        FLIGHT_HELMET = 32768,
        HIDE_IN_FIRST_PERSON = 65536,
        USE_PHYSICS_HAT_2 = 131072,
        PILOT_HELMET = 262144,
        WET_MORE_WET = 524288,
        WET_LESS_WET = 1048576,
    }
}
