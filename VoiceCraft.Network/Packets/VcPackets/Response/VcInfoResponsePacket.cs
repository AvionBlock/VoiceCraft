using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Response;

public class VcInfoResponsePacket(
    string motd,
    int clients,
    PositioningType positioningType,
    int tick,
    Version version)
    : IVoiceCraftPacket
{
    public VcInfoResponsePacket() : this(string.Empty, 0, PositioningType.Server, 0, new Version(0, 0, 0))
    {
    }

    public string Motd { get; private set; } = motd;
    public int Clients { get; private set; } = clients;
    public PositioningType PositioningType { get; private set; } = positioningType;
    public int Tick { get; private set; } = tick;
    public Version Version { get; private set; } = version;

    public VcPacketType PacketType => VcPacketType.InfoResponse;


    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Motd, Constants.MaxStringLength);
        writer.Put(Clients);
        writer.Put((byte)PositioningType);
        writer.Put(Tick);
        writer.Put(Version.Major);
        writer.Put(Version.Minor);
        writer.Put(Version.Build);
    }

    public void Deserialize(NetDataReader reader)
    {
        Motd = reader.GetString(Constants.MaxStringLength);
        Clients = reader.GetInt();
        PositioningType = (PositioningType)reader.GetByte();
        Tick = reader.GetInt();
        Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
    }

    public VcInfoResponsePacket Set(string motd = "", int clients = 0,
        PositioningType positioningType = PositioningType.Server,
        int tick = 0, Version? version = null)
    {
        Motd = motd;
        Clients = clients;
        PositioningType = positioningType;
        Tick = tick;
        Version = version ?? new Version(0, 0, 0);
        return this;
    }
}