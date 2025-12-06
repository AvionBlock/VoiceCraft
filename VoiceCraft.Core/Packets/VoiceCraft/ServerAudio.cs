using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class ServerAudio : VoiceCraftPacket, IDisposable
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.ServerAudio;
    public override bool IsReliable => false;

    public short Key { get; set; }
    public uint PacketCount { get; set; }
    public byte ChannelId { get; set; }
    public float Distance { get; set; }
    public Vector3 Location { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Performance critical buffer access")]
    public byte[] Audio { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The actual length of valid audio data in the Audio buffer.
    /// </summary>
    public int DataLength { get; set; }

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        
        int offset = sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence

        // Read Key (short)
        Key = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(offset));
        offset += sizeof(short);

        // Read PacketCount (uint)
        PacketCount = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(uint);

        // Read ChannelId (byte)
        ChannelId = buffer[offset++];

        // Read Distance (float)
        Distance = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);

        // Read Location (Vector3)
        float x = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        float y = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        float z = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        Location = new Vector3(x, y, z);

        // Read Audio Length (int)
        var audioLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (audioLength > 0)
        {
            if (Audio.Length > 0) ArrayPool<byte>.Shared.Return(Audio);
            Audio = ArrayPool<byte>.Shared.Rent(audioLength);
            buffer.Slice(offset, audioLength).CopyTo(Audio);
            DataLength = audioLength;
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        
        int offset = 1; // PacketId
        offset += sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence

        // Write Key
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), Key);
        offset += sizeof(short);

        // Write PacketCount
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset), PacketCount);
        offset += sizeof(uint);

        // Write ChannelId
        buffer[offset++] = ChannelId;

        // Write Distance
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Distance);
        offset += sizeof(float);

        // Write Location
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Location.X);
        offset += sizeof(float);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Location.Y);
        offset += sizeof(float);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Location.Z);
        offset += sizeof(float);

        // Write Audio
        var len = DataLength > 0 ? DataLength : Audio.Length;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), len);
        offset += sizeof(int);

        if (len > 0)
        {
            Audio.AsSpan(0, len).CopyTo(buffer.Slice(offset));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Audio != null)
            {
                if (Audio.Length > 0)
                    ArrayPool<byte>.Shared.Return(Audio);
                Audio = Array.Empty<byte>();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
