using VoiceCraft.Core.Network;

namespace VoiceCraft.Server
{
    public class ServerProperties
    {
        public bool Discovery { get; set; } = false;
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public PositioningType PositioningType { get; set; } = PositioningType.Client;
    }
}