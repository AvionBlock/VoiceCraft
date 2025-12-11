using VoiceCraft.Core;
using VoiceCraft.Core.Network.VcPackets.Response;

namespace VoiceCraft.Client.Network;

public struct ServerInfo
{
    public string Motd { get; set; }
    public int Clients { get; set; }
    public PositioningType PositioningType { get; set; }
    public int Tick { get; set; }

    public ServerInfo(VcInfoResponsePacket infoPacket)
    {
        Motd = infoPacket.Motd;
        Clients = infoPacket.Clients;
        PositioningType = infoPacket.PositioningType;
        Tick = infoPacket.Tick;
    }
}