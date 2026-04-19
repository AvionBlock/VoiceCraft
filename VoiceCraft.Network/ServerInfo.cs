using System;
using VoiceCraft.Network.Packets.VcPackets.Response;

namespace VoiceCraft.Network;

public readonly struct ServerInfo(VcInfoResponsePacket infoPacket)
{
    public string Motd { get; } = infoPacket.Motd;
    public int Clients { get; } = infoPacket.Clients;
    public PositioningType PositioningType { get; } = infoPacket.PositioningType;
    public int Tick { get; } = infoPacket.Tick;
    public Version Version { get; } = infoPacket.Version;
}