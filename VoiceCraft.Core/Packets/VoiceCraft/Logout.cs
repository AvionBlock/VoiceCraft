using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class Logout : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.Logout;
    public override bool IsReliable => false;

    //Packet Variables
    public string Reason { get; set; } = string.Empty;

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        // Offset: Id(8) = 8
        int offset = 8;

        int reasonLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (reasonLength > 0)
        {
            Reason = Encoding.UTF8.GetString(buffer.Slice(offset, reasonLength));
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        // Offset: Type(1) + Id(8) = 9
        int offset = 9;

        int reasonBytes = Encoding.UTF8.GetByteCount(Reason);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), reasonBytes);
        offset += sizeof(int);

        if (reasonBytes > 0)
        {
            Encoding.UTF8.GetBytes(Reason, buffer.Slice(offset));
        }
    }
}