using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace VoiceCraft.Core.Packets.VoiceCraft;

public class Login : VoiceCraftPacket
{
    public override byte PacketId => (byte)VoiceCraftPacketTypes.Login;
    public override bool IsReliable => true;

    //Packet Variables
    public short Key { get; set; }
    public PositioningTypes PositioningType { get; set; }
    public string Version { get; set; } = string.Empty;

    public override void Read(ReadOnlySpan<byte> buffer)
    {
        base.Read(buffer);
        
        int offset = sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence

        Key = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(offset));
        offset += sizeof(short);

        PositioningType = (PositioningTypes)buffer[offset];
        offset++;

        int versionLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
        offset += sizeof(int);

        if (versionLength > 0)
        {
            Version = Encoding.UTF8.GetString(buffer.Slice(offset, versionLength));
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        
        int offset = 1; // PacketId
        offset += sizeof(long); // Id
        if (IsReliable) offset += sizeof(uint); // Sequence

        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), Key);
        offset += sizeof(short);

        buffer[offset++] = (byte)PositioningType;

        int versionBytes = Encoding.UTF8.GetByteCount(Version);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), versionBytes);
        offset += sizeof(int);

        if (versionBytes > 0)
        {
            Encoding.UTF8.GetBytes(Version, buffer.Slice(offset));
        }
    }
}
