using VoiceCraft.Core.Packets;
using System.Collections.ObjectModel;

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
    /// Gets the filter for inbound VoiceCraft packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public Collection<VoiceCraftPacketTypes> InboundPacketFilter { get; } = [];

    /// <summary>
    /// Gets the filter for outbound VoiceCraft packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public Collection<VoiceCraftPacketTypes> OutboundPacketFilter { get; } = [];

    /// <summary>
    /// Gets the filter for inbound MCComm packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public Collection<MCCommPacketTypes> InboundMCCommFilter { get; } = [];

    /// <summary>
    /// Gets the filter for outbound MCComm packet types to log.
    /// Empty list means log all types.
    /// </summary>
    public Collection<MCCommPacketTypes> OutboundMCCommFilter { get; } = [];
}
