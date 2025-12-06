using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class RemoveChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.RemoveChannel;
        public override bool IsReliable => true;

        public byte ChannelId { get; set; }

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            // Offset: Id(8) + Sequence(4) = 12
            ChannelId = buffer[12];
        }

        public override void Write(Span<byte> buffer)
        {
            base.Write(buffer);
            // Offset: Type(1) + Id(8) + Sequence(4) = 13
            buffer[13] = ChannelId;
        }
    }
}
