using VoiceCraft.Core.Data;

namespace VoiceCraft.Core
{
    public class ServerProperties
    {
        public uint UpdateIntervalMs { get; set; } = 10; //10ms per update.
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public PositioningType PositioningType { get; set; } = PositioningType.Client;
    }
}