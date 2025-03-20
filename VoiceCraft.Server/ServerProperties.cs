using VoiceCraft.Core.Network;

namespace VoiceCraft.Server
{
    public class ServerProperties
    {
        public uint Port { get; set; } = 9050;
        public bool Discovery { get; set; } = false;
        public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
        public PositioningType PositioningType { get; set; } = PositioningType.Client;

        public static ServerProperties Load(string path)
        {
            Task.Delay(1000).Wait(); //Simulation
            return new ServerProperties();
        }
    }
}