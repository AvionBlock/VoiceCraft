namespace VoiceCraft.Core.Packets;

/// <summary>
/// Abstract base class for all VoiceCraft network packets.
/// Provides common serialization/deserialization logic for the custom binary protocol.
/// </summary>
public abstract class VoiceCraftPacket
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
    public int Retries;

    /// <summary>
    /// Reads packet data from a byte array.
    /// </summary>
    /// <param name="dataStream">The raw data.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <returns>The new offset after reading.</returns>
    public virtual int ReadPacket(ref byte[] dataStream, int offset = 0)
    {
        Id = BitConverter.ToInt64(dataStream, offset);
        offset += sizeof(long);

        if (IsReliable)
        {
            Sequence = BitConverter.ToUInt32(dataStream, offset);
            offset += sizeof(uint);
        }

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
        dataStream.AddRange(BitConverter.GetBytes(Id));

        if (IsReliable)
            dataStream.AddRange(BitConverter.GetBytes(Sequence));
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
public enum VoiceCraftPacketTypes : byte
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
