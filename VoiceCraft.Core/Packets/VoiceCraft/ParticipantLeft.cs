using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class ParticipantLeft : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ParticipantLeft;
        public override bool IsReliable => true;

        public short Key { get; set; }

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            // Offset: Id(8) + Sequence(4) = 12
            Key = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(12));
        }

        public override void Write(Span<byte> buffer)
        {
            base.Write(buffer);
            // Offset: Type(1) + Id(8) + Sequence(4) = 13
            BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(13), Key);
        }
    }
}
