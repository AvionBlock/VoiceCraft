using System;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Client.Network;

public struct ServerInfo
{
    public string Motd { get; set; }
    public int Clients { get; set; }
    public PositioningType PositioningType { get; set; }
    public int Tick { get; set; }
    public Version Version { get; set; }

    public ServerInfo(VcInfoResponsePacket infoPacket)
    {
        Motd = infoPacket.Motd;
        Clients = infoPacket.Clients;
        PositioningType = infoPacket.PositioningType;
        Tick = infoPacket.Tick;
        Version = infoPacket.Version;
    }
}