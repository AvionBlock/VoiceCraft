namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetChannelSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetChannelSettings;
        public byte ChannelId { get; set; }
        public int ProximityDistance { get; set; }
        public bool ProximityToggle { get; set; }
        public bool VoiceEffects { get; set; }
        public bool ClearSettings { get; set; }
    }
}
