namespace VoiceCraft.Core.Packets;

/// <summary>
/// Abstract base class for custom client packets used for local network discovery.
/// These are binary-serialized packets sent via UDP for client-to-client communication.
/// </summary>
public abstract class CustomClientPacket
{
    /// <summary>
    /// Gets the unique packet type identifier.
    /// </summary>
    public abstract byte PacketId { get; }

    /// <summary>
    /// Reads packet data from a byte array.
    /// </summary>
    /// <param name="dataStream">The raw data.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <returns>The new offset after reading.</returns>
    public virtual int ReadPacket(ref byte[] dataStream, int offset = 0)
    {
        return offset;
    }

    /// <summary>
    /// Writes packet data to a byte list.
    /// </summary>
    /// <param name="dataStream">The list to write to.</param>
    public virtual void WritePacket(ref List<byte> dataStream)
    {
        dataStream.Clear();
        dataStream.Add(PacketId);
    }
}

/// <summary>
/// Enumeration of custom client packet types.
/// </summary>
public enum CustomClientTypes : byte
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

