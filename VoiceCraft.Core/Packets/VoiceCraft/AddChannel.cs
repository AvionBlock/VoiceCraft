using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class AddChannel : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.AddChannel;
    public override bool IsReliable => true;

    public bool RequiresPassword { get; set; }
    public byte ChannelId { get; set; }
    public bool Locked { get; set; }
    public string Name { get; set; } = string.Empty;

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        // Offset: Id(8) + Sequence(4) = 12
        int offset = 12;

        RequiresPassword = buffer[offset++] != 0;
        ChannelId = buffer[offset++];
        Locked = buffer[offset++] != 0;

        int nameLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (nameLength > 0)
        {
            Name = Encoding.UTF8.GetString(buffer.Slice(offset, nameLength));
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        // Offset: Type(1) + Id(8) + Sequence(4) = 13
        int offset = 13;

        buffer[offset++] = RequiresPassword ? (byte)1 : (byte)0;
        buffer[offset++] = ChannelId;
        buffer[offset++] = Locked ? (byte)1 : (byte)0;

        int nameBytes = Encoding.UTF8.GetByteCount(Name);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), nameBytes);
        offset += sizeof(int);

        if (nameBytes > 0)
        {
            Encoding.UTF8.GetBytes(Name, buffer.Slice(offset));
        }
    }
}
