namespace VoiceCraft.Core.Packets;

/// <summary>
/// Defines the contract for all network packets in the VoiceCraft system.
/// </summary>
public interface IPacket
{
    /// <summary>
    /// Gets the unique packet type identifier.
    /// </summary>
    byte PacketId { get; }

    /// <summary>
    /// Writes the packet data to the specified buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    void Write(Span<byte> buffer);

    /// <summary>
    /// Reads the packet data from the specified buffer.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    void Read(ReadOnlySpan<byte> buffer);
}
