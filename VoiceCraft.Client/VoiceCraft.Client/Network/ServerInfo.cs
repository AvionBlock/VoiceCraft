using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public struct ServerInfo
    {
        public string Motd { get; set; }
        public int Clients { get; set; }
        public bool Discovery  { get; set; }
        public PositioningType PositioningType { get; set; }
        public int Tick { get; set; }

        public ServerInfo(InfoPacket infoPacket)
        {
            Motd = infoPacket.Motd;
            Clients = infoPacket.Clients;
            Discovery = infoPacket.Discovery;
            PositioningType = infoPacket.PositioningType;
            Tick = infoPacket.Tick;
        }
    }
}