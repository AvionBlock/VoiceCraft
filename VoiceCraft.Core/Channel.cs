namespace VoiceCraft.Core;

/// <summary>
/// Represents a voice chat channel that participants can join.
/// </summary>
public sealed class Channel
{
    /// <summary>
    /// Gets or sets the display name of the channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password required to join the channel.
    /// Empty string means no password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the channel is locked.
    /// A channel is also considered locked if it is hidden.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Gets or sets whether the channel is hidden from the channel list.
    /// Hidden channels are also implicitly locked.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets optional override settings for this channel.
    /// When null, default server settings are used.
    /// </summary>
    public ChannelOverride? OverrideSettings { get; set; }

    /// <summary>
    /// Gets the effective lock state, considering both the explicit Locked property and Hidden state.
    /// </summary>
    public bool IsLocked => Locked || Hidden;
}

/// <summary>
/// Contains optional override settings for a specific channel.
/// </summary>
public sealed class ChannelOverride
{
    /// <summary>
    /// Gets or sets the proximity distance for this channel.
    /// Players further than this distance won't hear each other.
    /// </summary>
    public int ProximityDistance { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether proximity-based audio is enabled.
    /// </summary>
    public bool ProximityToggle { get; set; } = true;

    /// <summary>
    /// Gets or sets whether voice effects (lowpass, echo) are enabled.
    /// </summary>
    public bool VoiceEffects { get; set; } = true;
}

