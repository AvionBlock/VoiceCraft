using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class UpdatePosition : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.UpdatePosition;
    public override bool IsReliable => false;

    public Vector3 Position { get; set; }

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        
        // Base consumes sizeof(long) [Id] + optional Sequence.
        // Since base doesn't track offset via ref, we must calculate it.
        // Packet structure: [PacketId(1)] [Id(8)] [Sequence(4)?] [Position(12)]
        
        // Calculate base offset
        int offset = sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence
        
        // Read Position (3 * float)
        float x = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        
        float y = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        
        float z = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        
        Position = new Vector3(x, y, z);
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        
        int offset = 1; // PacketId
        offset += sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence
        
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.X);
        offset += sizeof(float);
        
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Y);
        offset += sizeof(float);
        
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Z);
    }
}