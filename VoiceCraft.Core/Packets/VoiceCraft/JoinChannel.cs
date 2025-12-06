using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class JoinChannel : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.JoinChannel;
    public override bool IsReliable => true;

    public byte ChannelId { get; set; }
    public string Password { get; set; } = string.Empty;

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        // Offset: Id(8) + Sequence(4) = 12
        int offset = 12;

        ChannelId = buffer[offset++];

        int passwordLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (passwordLength > 0)
        {
            Password = Encoding.UTF8.GetString(buffer.Slice(offset, passwordLength));
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        // Offset: Type(1) + Id(8) + Sequence(4) = 13
        int offset = 13;

        buffer[offset++] = ChannelId;

        int passwordBytes = Encoding.UTF8.GetByteCount(Password);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), passwordBytes);
        offset += sizeof(int);

        if (passwordBytes > 0)
        {
            Encoding.UTF8.GetBytes(Password, buffer.Slice(offset));
        }
    }
}
