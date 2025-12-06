using System.Collections.Generic;
using System.Text;
using System;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Deny : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Deny;

        public string Reason { get; set; } = string.Empty;

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            int offset = 0;

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
            int offset = 1;

            int reasonBytes = Encoding.UTF8.GetByteCount(Reason);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), reasonBytes);
            offset += sizeof(int);

            if (reasonBytes > 0)
            {
                Encoding.UTF8.GetBytes(Reason, buffer.Slice(offset));
            }
        }
    }
}
