namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelMove : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ChannelMove;
        public string PlayerId { get; set; } = string.Empty;
        public byte ChannelId { get; set; } // Default is 0
    }
}
