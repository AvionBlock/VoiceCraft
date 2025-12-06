using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Ack : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Ack;
        public override bool IsReliable => false;

        public uint PacketSequence { get; set; }

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            // Offset: Id(8) = 8
            PacketSequence = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8));
        }

        public override void Write(Span<byte> buffer)
        {
            base.Write(buffer);
            // Offset: Type(1) + Id(8) = 9
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(9), PacketSequence);
        }
    }
}
