using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Login : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Login;

        public string Name { get; set; } = string.Empty;

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            int offset = 0;

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
            int offset = 1;

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
