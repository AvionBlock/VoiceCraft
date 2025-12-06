using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class PingInfo : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.PingInfo;
    public override bool IsReliable => false;

    public PositioningTypes PositioningType { get; set; }
    public int ConnectedParticipants { get; set; }
    public string MOTD { get; set; } = string.Empty;

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        // Offset: Id(8) = 8
        int offset = 8;
        
        PositioningType = (PositioningTypes)buffer[offset];
        offset++;

        ConnectedParticipants = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        int motdLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (motdLength > 0)
        {
            MOTD = Encoding.UTF8.GetString(buffer.Slice(offset, motdLength));
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        // Offset: Type(1) + Id(8) = 9
        int offset = 9;

        buffer[offset++] = (byte)PositioningType;

        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), ConnectedParticipants);
        offset += sizeof(int);

        int motdBytes = Encoding.UTF8.GetByteCount(MOTD);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), motdBytes);
        offset += sizeof(int);

        if (motdBytes > 0)
        {
            Encoding.UTF8.GetBytes(MOTD, buffer.Slice(offset));
        }
    }
}
