using System.Collections.Generic;
using System.Text;
using System;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class UpdateEnvironmentId : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.UpdateEnvironmentId;
        public override bool IsReliable => true;

        public string EnvironmentId { get; set; } = string.Empty;

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            // Offset: Id(8) + Sequence(4) = 12
            int offset = 12;

            int environmentIdLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
            offset += sizeof(int);

            if (environmentIdLength > 0)
            {
                EnvironmentId = Encoding.UTF8.GetString(buffer.Slice(offset, environmentIdLength));
            }
        }

        public override void Write(Span<byte> buffer)
        {
            base.Write(buffer);
            // Offset: Type(1) + Id(8) + Sequence(4) = 13
            int offset = 13;

            int environmentIdBytes = Encoding.UTF8.GetByteCount(EnvironmentId);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), environmentIdBytes);
            offset += sizeof(int);

            if (environmentIdBytes > 0)
            {
                Encoding.UTF8.GetBytes(EnvironmentId, buffer.Slice(offset));
            }
        }
    }
}
