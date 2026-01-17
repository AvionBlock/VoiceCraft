using System;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network;

public struct ServerInfo(VcInfoResponsePacket infoPacket)
{
    public string Motd { get; set; } = infoPacket.Motd;
    public int Clients { get; set; } = infoPacket.Clients;
    public PositioningType PositioningType { get; set; } = infoPacket.PositioningType;
    public int Tick { get; set; } = infoPacket.Tick;
    public Version Version { get; set; } = infoPacket.Version;
}