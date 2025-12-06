using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class FullUpdatePosition : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.FullUpdatePosition;
    public override bool IsReliable => false;

    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public float EchoFactor { get; set; }
    public bool Muffled { get; set; }
    public bool IsDead { get; set; }

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        // Offset: Id(8) = 8
        int offset = 8;
        
        float x = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        float y = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        float z = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);
        Position = new Vector3(x, y, z);

        Rotation = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);

        EchoFactor = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
        offset += sizeof(float);

        Muffled = buffer[offset++] != 0;
        IsDead = buffer[offset++] != 0;
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        // Offset: Type(1) + Id(8) = 9
        int offset = 9;

        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.X);
        offset += sizeof(float);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Y);
        offset += sizeof(float);
        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Z);
        offset += sizeof(float);

        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Rotation);
        offset += sizeof(float);

        BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), EchoFactor);
        offset += sizeof(float);

        buffer[offset++] = Muffled ? (byte)1 : (byte)0;
        buffer[offset++] = IsDead ? (byte)1 : (byte)0;
    }
}