using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core;

/// <summary>
/// Configuration properties for debugging and logging packet traffic.
/// </summary>
public class DebugProperties
{
    /// <summary>
    /// Gets or sets whether to log exceptions.
    /// </summary>
    public bool LogExceptions { get; set; }

    /// <summary>
    /// Gets or sets whether to log inbound VoiceCraft packets.
    /// </summary>
    public bool LogInboundPackets { get; set; }

    /// <summary>
    /// Gets or sets whether to log outbound VoiceCraft packets.
    /// </summary>
    public bool LogOutboundPackets { get; set; }

    /// <summary>
    /// Gets or sets whether to log inbound MCComm packets.
    /// </summary>
    public bool LogInboundMCCommPackets { get; set; }

    /// <summary>
    /// Gets or sets whether to log outbound MCComm packets.
    /// </summary>
    public bool LogOutboundMCCommPackets { get; set; }

    /// <summary>
    /// Gets or sets the filter for inbound VoiceCraft packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public List<VoiceCraftPacketTypes> InboundPacketFilter { get; set; } = [];

    /// <summary>
    /// Gets or sets the filter for outbound VoiceCraft packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public List<VoiceCraftPacketTypes> OutboundPacketFilter { get; set; } = [];

    /// <summary>
    /// Gets or sets the filter for inbound MCComm packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public List<MCCommPacketTypes> InboundMCCommFilter { get; set; } = [];

    /// <summary>
    /// Gets or sets the filter for outbound MCComm packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public List<MCCommPacketTypes> OutboundMCCommFilter { get; set; } = [];
}
