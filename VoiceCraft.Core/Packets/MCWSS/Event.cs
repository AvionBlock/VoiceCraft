namespace VoiceCraft.Core.Packets.MCWSS;

/// <summary>
/// Represents an event subscription body for Minecraft WebSocket events.
/// </summary>
public class MCWSSEvent
{
    /// <summary>Gets or sets the event name to subscribe to.</summary>
    public string eventName { get; set; } = string.Empty;
}
