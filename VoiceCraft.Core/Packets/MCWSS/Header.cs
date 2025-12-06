namespace VoiceCraft.Core.Packets.MCWSS;

/// <summary>
/// WebSocket message header for Minecraft Bedrock Edition communication.
/// </summary>
public class Header
{
    /// <summary>Gets or sets the request ID.</summary>
    public string requestId { get; set; } = string.Empty;

    /// <summary>Gets or sets the message purpose.</summary>
    public string messagePurpose { get; set; } = string.Empty;

    /// <summary>Gets or sets the protocol version.</summary>
    public int version { get; set; } = 1;

    /// <summary>Gets or sets the message type.</summary>
    public string messageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the event name.</summary>
    public string eventName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is Header other)
        {
            return string.Equals(messagePurpose, other.messagePurpose, StringComparison.Ordinal) &&
                   string.Equals(messageType, other.messageType, StringComparison.Ordinal) &&
                   string.Equals(eventName, other.eventName, StringComparison.Ordinal);
        }
        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(messagePurpose, messageType, eventName);
}

