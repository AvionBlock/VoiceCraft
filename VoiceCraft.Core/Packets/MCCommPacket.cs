using Newtonsoft.Json;

namespace VoiceCraft.Core.Packets;

/// <summary>
/// Abstract base class for Minecraft Communication (MCComm) packets.
/// These are JSON-serialized packets used for server-client plugin communication via HTTP.
/// </summary>
public abstract class MCCommPacket
{
    /// <summary>
    /// Gets the unique packet type identifier.
    /// </summary>
    public abstract byte PacketId { get; }

    /// <summary>
    /// Gets or sets the authentication token for this session.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Serializes this packet to a JSON string.
    /// </summary>
    /// <returns>The JSON representation of this packet.</returns>
    public virtual string SerializePacket() => JsonConvert.SerializeObject(this);
}

/// <summary>
/// Enumeration of all MCComm packet types.
/// </summary>
public enum MCCommPacketTypes : int
{
    /// <summary>Login request packet.</summary>
    Login,
    /// <summary>Logout request packet.</summary>
    Logout,
    /// <summary>Accept response packet.</summary>
    Accept,
    /// <summary>Deny response packet.</summary>
    Deny,
    /// <summary>Bind player to session.</summary>
    Bind,
    /// <summary>Position/state update.</summary>
    Update,
    /// <summary>Acknowledge update received.</summary>
    AckUpdate,
    /// <summary>Request channel list.</summary>
    GetChannels,
    /// <summary>Get channel settings.</summary>
    GetChannelSettings,
    /// <summary>Set channel settings.</summary>
    SetChannelSettings,
    /// <summary>Get default server settings.</summary>
    GetDefaultSettings,
    /// <summary>Set default server settings.</summary>
    SetDefaultSettings,

    // Participant Management
    /// <summary>Get participant list.</summary>
    GetParticipants,
    /// <summary>Disconnect a participant.</summary>
    DisconnectParticipant,
    /// <summary>Get participant permissions bitmask.</summary>
    GetParticipantBitmask,
    /// <summary>Set participant permissions bitmask.</summary>
    SetParticipantBitmask,
    /// <summary>Mute a participant.</summary>
    MuteParticipant,
    /// <summary>Unmute a participant.</summary>
    UnmuteParticipant,
    /// <summary>Deafen a participant.</summary>
    DeafenParticipant,
    /// <summary>Undeafen a participant.</summary>
    UndeafenParticipant,

    // Bitmask Operations
    /// <summary>AND operation on participant bitmask.</summary>
    ANDModParticipantBitmask,
    /// <summary>OR operation on participant bitmask.</summary>
    ORModParticipantBitmask,
    /// <summary>XOR operation on participant bitmask.</summary>
    XORModParticipantBitmask,

    /// <summary>Move participant to different channel.</summary>
    ChannelMove
}
