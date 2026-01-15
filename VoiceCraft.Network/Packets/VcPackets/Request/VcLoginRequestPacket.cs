using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VcPackets.Request;

public class VcLoginRequestPacket : IVoiceCraftPacket, IVoiceCraftRIdPacket
{
    public VcLoginRequestPacket() : this(Guid.Empty, Guid.Empty, Guid.Empty, string.Empty, new Version(0, 0, 0),
        PositioningType.Server)
    {
    }

    public VcLoginRequestPacket(
        Guid requestId,
        Guid userGuid,
        Guid serverUserGuid,
        string locale,
        Version version,
        PositioningType positioningType)
    {
        RequestId = requestId;
        UserGuid = userGuid;
        ServerUserGuid = serverUserGuid;
        Locale = locale;
        Version = version;
        PositioningType = positioningType;
    }

    public Guid UserGuid { get; private set; }
    public Guid ServerUserGuid { get; private set; }
    public string Locale { get; private set; }
    public Version Version { get; private set; }
    public PositioningType PositioningType { get; private set; }

    public VcPacketType PacketType => VcPacketType.LoginRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId);
        writer.Put(UserGuid);
        writer.Put(ServerUserGuid);
        writer.Put(Locale, Constants.MaxStringLength);
        writer.Put(Version.Major);
        writer.Put(Version.Minor);
        writer.Put(Version.Build);
        writer.Put((byte)PositioningType);
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetGuid();
        UserGuid = reader.GetGuid();
        ServerUserGuid = reader.GetGuid();
        Locale = reader.GetString(Constants.MaxStringLength);
        Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
        PositioningType = (PositioningType)reader.GetByte();
    }

    public Guid RequestId { get; private set; }

    public VcLoginRequestPacket Set(
        Guid requestId = new(),
        Guid userGuid = new(),
        Guid serverUserGuid = new(),
        string locale = "",
        Version? version = null,
        PositioningType positioningType = PositioningType.Server)
    {
        RequestId = requestId;
        UserGuid = userGuid;
        ServerUserGuid = serverUserGuid;
        Locale = locale;
        Version = version ?? new Version(0, 0, 0);
        PositioningType = positioningType;
        return this;
    }
}