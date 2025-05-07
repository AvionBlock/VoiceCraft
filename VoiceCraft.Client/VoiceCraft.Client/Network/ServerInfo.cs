using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.Network
{
    public struct ServerInfo
    {
        public string Motd { get; set; }
        public int Clients { get; set; }
        public PositioningType PositioningType { get; set; }
        public int Tick { get; set; }

        public ServerInfo(InfoPacket infoPacket)
        {
            Motd = infoPacket.Motd;
            Clients = infoPacket.Clients;
            PositioningType = infoPacket.PositioningType;
            Tick = infoPacket.Tick;
        }
    }
}