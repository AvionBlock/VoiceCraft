namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetDefaultSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetDefaultSettings;
        public int ProximityDistance { get; set; }
        public bool ProximityToggle { get; set; }
        public bool VoiceEffects { get; set; }
    }
}
