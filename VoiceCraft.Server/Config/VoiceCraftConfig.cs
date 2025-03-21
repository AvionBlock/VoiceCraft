using VoiceCraft.Core.Network;

namespace VoiceCraft.Server.Config
{
    public class VoiceCraftConfig
    {
        public uint Port { get; set; } = 9050;
        public bool Discovery { get; set; } = false;
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public PositioningType PositioningType { get; set; } = PositioningType.Client;
    }
}