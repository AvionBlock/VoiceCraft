namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetDefaultSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetDefaultSettings;
        public int ProximityDistance { get; set; }
        public bool ProximityToggle { get; set; }
        public bool VoiceEffects { get; set; }
    }
}
