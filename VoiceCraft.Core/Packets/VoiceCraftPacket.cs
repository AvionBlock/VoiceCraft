using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets;

/// <summary>
/// Abstract base class for all VoiceCraft network packets.
/// Provides common serialization/deserialization logic for the custom binary protocol.
/// </summary>
public abstract class VoiceCraftPacket : IPacket
{
    /// <summary>
    /// Gets the unique packet type identifier.
    /// </summary>
    public abstract byte PacketId { get; }

    /// <summary>
    /// Gets whether this packet requires reliable delivery with acknowledgment.
    /// </summary>
    public abstract bool IsReliable { get; }

    /// <summary>
    /// Gets or sets the sequence number for reliable packets.
    /// </summary>
    public uint Sequence { get; set; }

    /// <summary>
    /// Gets or sets the unique connection identifier.
    /// </summary>
    public long Id { get; set; } = long.MinValue;

    /// <summary>
    /// Gets or sets the time when this packet should be resent if not acknowledged.
    /// </summary>
    public long ResendTime { get; set; }

    /// <summary>
    /// Number of retry attempts for reliable packets. Used atomically with Interlocked.
    /// </summary>
    private int _retries;

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    public int Retries
    {
        get => _retries;
        set => _retries = value;
    }

    /// <summary>
    /// Atomically increments the retry count.
    /// </summary>
    /// <returns>The incremented value.</returns>
    public int IncrementRetries()
    {
        return System.Threading.Interlocked.Increment(ref _retries);
    }

    /// <summary>
    /// Reads packet data from a read-only byte span.
    /// </summary>
    /// <param name="buffer">The source buffer.</param>
    public virtual void Read(ReadOnlySpan<byte> buffer)
    {
        int offset = 0;
        
        // Read Id (long)
        Id = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(offset));
        offset += sizeof(long);

        if (IsReliable)
        {
            // Read Sequence (uint)
            Sequence = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset));
            // offset += sizeof(uint); // Not strictly needed if this is the end
        }
    }

    /// <summary>
    /// Writes packet data to a byte span.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    public virtual void Write(Span<byte> buffer)
    {
        int offset = 0;
        
        // Write PacketId (byte)
        buffer[offset++] = PacketId;

        // Write Id (long)
        BinaryPrimitives.WriteInt64LittleEndian(buffer.Slice(offset), Id);
        offset += sizeof(long);

        if (IsReliable)
        {
            // Write Sequence (uint)
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset), Sequence);
            // offset += sizeof(uint);
        }
    }

    /// <summary>
    /// Creates a shallow copy of this packet.
    /// </summary>
    /// <returns>A cloned packet.</returns>
    public VoiceCraftPacket Clone() => (VoiceCraftPacket)MemberwiseClone();
}

/// <summary>
/// Enumeration of all VoiceCraft packet types.
/// </summary>
public enum VoiceCraftPacketTypes : int
{
    // System/Protocol Packets
    /// <summary>Login request packet.</summary>
    Login,
    /// <summary>Logout notification packet.</summary>
    Logout,
    /// <summary>Accept response packet.</summary>
    Accept,
    /// <summary>Deny response packet.</summary>
    Deny,
    /// <summary>Acknowledgment packet for reliable delivery.</summary>
    Ack,
    /// <summary>Keep-alive ping packet.</summary>
    Ping,
    /// <summary>Server information response packet.</summary>
    PingInfo,

    // User Packets
    /// <summary>Player bound to Minecraft account.</summary>
    Binded,
    /// <summary>Player unbound from Minecraft account.</summary>
    Unbinded,
    /// <summary>New participant joined notification.</summary>
    ParticipantJoined,
    /// <summary>Participant left notification.</summary>
    ParticipantLeft,
    /// <summary>Mute notification.</summary>
    Mute,
    /// <summary>Unmute notification.</summary>
    Unmute,
    /// <summary>Deafen notification.</summary>
    Deafen,
    /// <summary>Undeafen notification.</summary>
    Undeafen,
    /// <summary>Join channel request/notification.</summary>
    JoinChannel,
    /// <summary>Leave channel request/notification.</summary>
    LeaveChannel,
    /// <summary>Channel added notification.</summary>
    AddChannel,
    /// <summary>Channel removed notification.</summary>
    RemoveChannel,

    // Voice Packets
    /// <summary>Position update (relative).</summary>
    UpdatePosition,
    /// <summary>Full position update (absolute).</summary>
    FullUpdatePosition,
    /// <summary>Environment/dimension update.</summary>
    UpdateEnvironmentId,
    /// <summary>Audio data from client.</summary>
    ClientAudio,
    /// <summary>Audio data from server.</summary>
    ServerAudio
}
