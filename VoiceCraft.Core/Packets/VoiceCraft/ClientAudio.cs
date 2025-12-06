using System;
using System.Buffers;
using System.Buffers.Binary;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class ClientAudio : VoiceCraftPacket, IDisposable
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.ClientAudio;
    public override bool IsReliable => false;

    public uint PacketCount { get; set; }
    
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

        PacketCount = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(uint);

        var audioLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (audioLength > 0)
        {
            // Return old buffer if it exists (though Read is usually called on new object)
            if (Audio.Length > 0) ArrayPool<byte>.Shared.Return(Audio);

            // Rent from pool
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

        BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset), PacketCount);
        offset += sizeof(uint);

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
