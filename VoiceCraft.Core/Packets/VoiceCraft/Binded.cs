using System.Collections.Generic;
using System.Text;
using System;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Binded : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Binded;
        public override bool IsReliable => true;

        public string Name { get; set; } = string.Empty;

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            // Offset: Id(8) + Sequence(4) = 12
            int offset = 12;

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

            int nameBytes = Encoding.UTF8.GetByteCount(Name);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), nameBytes);
            offset += sizeof(int);

            if (nameBytes > 0)
            {
                Encoding.UTF8.GetBytes(Name, buffer.Slice(offset));
            }
        }
    }
}
