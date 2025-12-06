namespace VoiceCraft.Core.Packets;

/// <summary>
/// Abstract base class for custom client packets used for local network discovery.
/// These are binary-serialized packets sent via UDP for client-to-client communication.
/// </summary>
public abstract class CustomClientPacket : IPacket
{
    /// <summary>
    /// Gets the unique packet type identifier.
    /// </summary>
    public abstract byte PacketId { get; }

    /// <summary>
    /// Reads packet data from a read-only byte span.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    public virtual void Read(ReadOnlySpan<byte> buffer)
    {
        // Default implementation does nothing (for empty packets)
    }

    /// <summary>
    /// Writes packet data to a byte span.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    public virtual void Write(Span<byte> buffer)
    {
        buffer[0] = PacketId;
    }
}

/// <summary>
/// Enumeration of custom client packet types.
/// </summary>
public enum CustomClientTypes : int
{
    /// <summary>Login request.</summary>
    Login,
    /// <summary>Logout notification.</summary>
    Logout,
    /// <summary>Accept response.</summary>
    Accept,
    /// <summary>Deny response.</summary>
    Deny,
    /// <summary>Position/state update.</summary>
    Update
}

