using System.Numerics;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data;

/// <summary>
/// Represents a participant in the VoiceCraft server with position, state, and permission bitmask tracking.
/// </summary>
public class VoiceCraftParticipant : Participant
{
    /// <summary>
    /// Gets or sets the unique key for this participant.
    /// </summary>
    public short Key { get; set; }

    /// <summary>
    /// Gets or sets whether this participant is bound to a Minecraft player.
    /// </summary>
    public bool Binded { get; set; }

    /// <summary>
    /// Gets or sets whether this participant uses client-side positioning.
    /// </summary>
    public bool ClientSided { get; set; }

    /// <summary>
    /// Gets or sets whether this participant is muted by the server.
    /// </summary>
    public bool ServerMuted { get; set; }

    /// <summary>
    /// Gets or sets whether this participant is deafened by the server.
    /// </summary>
    public bool ServerDeafened { get; set; }

    /// <summary>
    /// Gets or sets the channel this participant is in.
    /// </summary>
    public Channel Channel { get; set; }

    /// <summary>
    /// Gets or sets the participant's 3D position.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the participant's Y-axis rotation in degrees.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the echo factor for voice effects.
    /// </summary>
    public float EchoFactor { get; set; }

    /// <summary>
    /// Gets or sets whether the participant's character is dead.
    /// </summary>
    public bool Dead
    {
        get => ((ChecksBitmask >> (int)BitmaskLocations.DataBitmask) & (uint)DataBitmask.Dead) != 0;
        set
        {
            if (value)
                ChecksBitmask |= (uint)DataBitmask.Dead << (int)BitmaskLocations.DataBitmask;
            else
                ChecksBitmask &= ~((uint)DataBitmask.Dead << (int)BitmaskLocations.DataBitmask);
        }
    }

    /// <summary>
    /// Gets or sets whether the participant's voice is muffled.
    /// </summary>
    public bool Muffled
    {
        get => ((ChecksBitmask >> (int)BitmaskLocations.DataBitmask) & (uint)DataBitmask.Muffled) != 0;
        set
        {
            if (value)
                ChecksBitmask |= (uint)DataBitmask.Muffled << (int)BitmaskLocations.DataBitmask;
            else
                ChecksBitmask &= ~((uint)DataBitmask.Muffled << (int)BitmaskLocations.DataBitmask);
        }
    }

    /// <summary>
    /// Gets or sets the Minecraft dimension/environment ID.
    /// </summary>
    public string EnvironmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Minecraft player ID.
    /// </summary>
    public string MinecraftId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission bitmask for talk/listen settings.
    /// </summary>
    public uint ChecksBitmask { get; set; } = (uint)BitmaskMap.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceCraftParticipant"/> class.
    /// </summary>
    /// <param name="name">The participant's name.</param>
    /// <param name="channel">The initial channel.</param>
    public VoiceCraftParticipant(string name, Channel channel) : base(name)
    {
        Channel = channel;
    }

    /// <summary>
    /// Generates a random participant key.
    /// </summary>
    /// <returns>A random short value (excluding short.MinValue which is reserved).</returns>
    public static short GenerateKey()
    {
        return (short)Random.Shared.Next(short.MinValue + 1, short.MaxValue);
    }

    /// <summary>
    /// Gets the intersected talk bitmasks between this participant and another.
    /// </summary>
    public uint GetIntersectedTalkBitmasks(uint otherBitmask)
    {
        return ((ChecksBitmask & (uint)BitmaskMap.AllTalkBitmasks) >> (int)BitmaskLocations.TalkBitmask1) 
            & ((otherBitmask & (uint)BitmaskMap.AllListenBitmasks) >> (int)BitmaskLocations.ListenBitmask1);
    }

    /// <summary>
    /// Gets the intersected listen bitmasks between this participant and another.
    /// </summary>
    public uint GetIntersectedListenBitmasks(uint otherBitmask)
    {
        return ((ChecksBitmask & (uint)BitmaskMap.AllListenBitmasks) >> (int)BitmaskLocations.ListenBitmask1) 
            & ((otherBitmask & (uint)BitmaskMap.AllTalkBitmasks) >> (int)BitmaskLocations.TalkBitmask1);
    }

    /// <summary>
    /// Checks if any of the specified talk settings are disabled in the intersected bitmasks.
    /// </summary>
    public bool IntersectedTalkSettingsDisabled(uint otherBitmask, params BitmaskSettings[] settings)
    {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint intersectingBits = GetIntersectedTalkBitmasks(otherBitmask);
            uint disabledTalkMasks = intersectingBits << (int)BitmaskLocations.TalkBitmask1; //Move into the talk bitmask area.
            uint mask = disabledTalkMasks | (uint)BitmaskMap.AllBitmaskSettings; //Create the mask.
            uint talkSettings = GetDisabledTalkSettings(ChecksBitmask & mask); //Isolate all settings and disabled bitmasks and get the disabled talk settings.

            return (talkSettings & settingsMask) != 0; //check if any of the inputted settings match the combined settings.
        }

        public bool IntersectedListenSettingsDisabled(uint otherBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint intersectingBits = GetIntersectedListenBitmasks(otherBitmask);
            uint disabledListenMasks = intersectingBits << (int)BitmaskLocations.ListenBitmask1; //Move into the listen bitmask area.
            uint mask = disabledListenMasks | (uint)BitmaskMap.AllBitmaskSettings; //Create the mask.
            uint listenSettings = GetDisabledListenSettings(ChecksBitmask & mask); //Isolate all settings and disabled bitmasks and get the disabled listen settings.

            return (listenSettings & settingsMask) != 0; //check if any of the inputted settings match the combined settings.
        }

        public static uint GetDisabledTalkSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask5Settings;

            return result;
        }

        public static uint GetDisabledListenSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask5Settings;

            return result;
        }

        public static bool TalkSettingsDisabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask1Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask2Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask3Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask4Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask5Settings) & settingsMask;

            return result != 0;
        }

        public static bool ListenSettingsDisabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask1Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask2Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask3Settings) & settingsMask;

        if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
            result |= (checksBitmask >> (int)BitmaskLocations.Bitmask4Settings) & settingsMask;

        if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
            result |= (checksBitmask >> (int)BitmaskLocations.Bitmask5Settings) & settingsMask;

        return result != 0;
    }
}

/// <summary>
/// Bitmask mappings for participant permissions.
/// Uses a 32-bit structure for talk/listen settings and data flags.
/// </summary>
public enum BitmaskMap : uint
{
    /// <summary>Default bitmask with first talk and listen enabled.</summary>
    Default = TalkBitmask1 | ListenBitmask1,
    /// <summary>All settings bits combined.</summary>
    AllBitmaskSettings = Bitmask1Settings | Bitmask2Settings | Bitmask3Settings | Bitmask4Settings | Bitmask5Settings,
    /// <summary>All talk bitmask bits combined.</summary>
    AllTalkBitmasks = TalkBitmask1 | TalkBitmask2 | TalkBitmask3 | TalkBitmask4 | TalkBitmask5,
    /// <summary>All listen bitmask bits combined.</summary>
    AllListenBitmasks = ListenBitmask1 | ListenBitmask2 | ListenBitmask3 | ListenBitmask4 | ListenBitmask5,

    /// <summary>Settings for bitmask group 1 (bits 0-3).</summary>
    Bitmask1Settings = 0b00000000000000000000000000001111,
    /// <summary>Settings for bitmask group 2 (bits 4-7).</summary>
    Bitmask2Settings = 0b00000000000000000000000011110000,
    /// <summary>Settings for bitmask group 3 (bits 8-11).</summary>
    Bitmask3Settings = 0b00000000000000000000111100000000,
    /// <summary>Settings for bitmask group 4 (bits 12-15).</summary>
    Bitmask4Settings = 0b00000000000000001111000000000000,
    /// <summary>Settings for bitmask group 5 (bits 16-19).</summary>
    Bitmask5Settings = 0b00000000000011110000000000000000,
    /// <summary>Talk bitmask 1 (bit 20).</summary>
    TalkBitmask1 = 0b00000000000100000000000000000000,
    /// <summary>Talk bitmask 2 (bit 21).</summary>
    TalkBitmask2 = 0b00000000001000000000000000000000,
    /// <summary>Talk bitmask 3 (bit 22).</summary>
    TalkBitmask3 = 0b00000000010000000000000000000000,
    /// <summary>Talk bitmask 4 (bit 23).</summary>
    TalkBitmask4 = 0b00000000100000000000000000000000,
    /// <summary>Talk bitmask 5 (bit 24).</summary>
    TalkBitmask5 = 0b00000001000000000000000000000000,
    /// <summary>Listen bitmask 1 (bit 25).</summary>
    ListenBitmask1 = 0b00000010000000000000000000000000,
    /// <summary>Listen bitmask 2 (bit 26).</summary>
    ListenBitmask2 = 0b00000100000000000000000000000000,
    /// <summary>Listen bitmask 3 (bit 27).</summary>
    ListenBitmask3 = 0b00001000000000000000000000000000,
    /// <summary>Listen bitmask 4 (bit 28).</summary>
    ListenBitmask4 = 0b00010000000000000000000000000000,
    /// <summary>Listen bitmask 5 (bit 29).</summary>
    ListenBitmask5 = 0b00100000000000000000000000000000,
    /// <summary>Data bitmask (bits 30-31).</summary>
    DataBitmask = 0b11000000000000000000000000000000
}

/// <summary>
/// Bit positions for bitmask locations in the 32-bit check bitmask.
/// </summary>
public enum BitmaskLocations
{
    /// <summary>Bitmask 1 settings position (4 bits).</summary>
    Bitmask1Settings = 0,
    /// <summary>Bitmask 2 settings position (4 bits).</summary>
    Bitmask2Settings = 4,
    /// <summary>Bitmask 3 settings position (4 bits).</summary>
    Bitmask3Settings = 8,
    /// <summary>Bitmask 4 settings position (4 bits).</summary>
    Bitmask4Settings = 12,
    /// <summary>Bitmask 5 settings position (4 bits).</summary>
    Bitmask5Settings = 16,
    /// <summary>Talk bitmask 1 position (1 bit).</summary>
    TalkBitmask1 = 20,
    /// <summary>Talk bitmask 2 position (1 bit).</summary>
    TalkBitmask2 = 21,
    /// <summary>Talk bitmask 3 position (1 bit).</summary>
    TalkBitmask3 = 22,
    /// <summary>Talk bitmask 4 position (1 bit).</summary>
    TalkBitmask4 = 23,
    /// <summary>Talk bitmask 5 position (1 bit).</summary>
    TalkBitmask5 = 24,
    /// <summary>Listen bitmask 1 position (1 bit).</summary>
    ListenBitmask1 = 25,
    /// <summary>Listen bitmask 2 position (1 bit).</summary>
    ListenBitmask2 = 26,
    /// <summary>Listen bitmask 3 position (1 bit).</summary>
    ListenBitmask3 = 27,
    /// <summary>Listen bitmask 4 position (1 bit).</summary>
    ListenBitmask4 = 28,
    /// <summary>Listen bitmask 5 position (1 bit).</summary>
    ListenBitmask5 = 29,
    /// <summary>Data bitmask position (2 bits).</summary>
    DataBitmask = 30
}

/// <summary>
/// Settings flags that can be disabled per bitmask group.
/// </summary>
public enum BitmaskSettings : uint
{
    /// <summary>All settings enabled.</summary>
    All = uint.MaxValue,
    /// <summary>No settings enabled.</summary>
    None = 0,
    /// <summary>Proximity distance check disabled.</summary>
    ProximityDisabled = 1,
    /// <summary>Death check disabled.</summary>
    DeathDisabled = 2,
    /// <summary>Voice effects disabled.</summary>
    VoiceEffectsDisabled = 4,
    /// <summary>Environment check disabled.</summary>
    EnvironmentDisabled = 8
}

/// <summary>
/// Data flags stored in the data bitmask section.
/// </summary>
public enum DataBitmask : uint
{
    /// <summary>Participant's character is dead.</summary>
    Dead = 1,
    /// <summary>Participant's voice is muffled.</summary>
    Muffled = 2
}

