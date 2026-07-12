using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Packets.McApiPackets.Event;

public class McApiOnNetworkEntityCreatedPacket : McApiOnEntityCreatedPacket
{
    public McApiOnNetworkEntityCreatedPacket() : this(
        0, 
        0.0f, 
        DateTime.MinValue, 
        Guid.Empty, 
        Guid.Empty, 
        string.Empty,
        PositioningType.Server)
    {
    }

    public McApiOnNetworkEntityCreatedPacket(
        int id,
        float loudness,
        DateTime lastSpoke,
        Guid userGuid,
        Guid serverUserGuid,
        string locale,
        PositioningType positioningType) :
        base(id,
            loudness,
            lastSpoke)
    {
        UserGuid = userGuid;
        ServerUserGuid = serverUserGuid;
        Locale = locale;
        PositioningType = positioningType;
    }

    public McApiOnNetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
    {
        UserGuid = entity.UserGuid;
        ServerUserGuid = entity.ServerUserGuid;
        Locale = entity.Locale;
        PositioningType = entity.PositioningType;
    }

    public override EventType EventType => EventType.OnNetworkEntityCreated;
    public Guid UserGuid { get; private set; }
    public Guid ServerUserGuid { get; private set; }
    public string Locale { get; private set; }
    public PositioningType PositioningType { get; private set; }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(UserGuid.ToString(), Constants.MaxStringLength);
        writer.Put(ServerUserGuid.ToString(), Constants.MaxStringLength);
        writer.Put(Locale, Constants.MaxStringLength);
        writer.Put((byte)PositioningType);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        UserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
        ServerUserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
        Locale = reader.GetString(Constants.MaxStringLength);
        PositioningType = (PositioningType)reader.GetByte();
    }

    public override void Return()
    {
        PacketPool<McApiOnNetworkEntityCreatedPacket>.Return(this);
    }

    public void Set(
        int id = 0,
        float loudness = 0.0f,
        DateTime lastSpoke = new(),
        Guid userGuid = new(),
        Guid serverUserGuid = new(),
        string locale = "",
        PositioningType positioningType = PositioningType.Server)
    {
        base.Set(id, loudness, lastSpoke);
        UserGuid = userGuid;
        ServerUserGuid = serverUserGuid;
        Locale = locale;
        PositioningType = positioningType;
    }

    public void Set(VoiceCraftNetworkEntity entity)
    {
        base.Set(entity);
        UserGuid = entity.UserGuid;
        ServerUserGuid = entity.ServerUserGuid;
        Locale = entity.Locale;
        PositioningType = entity.PositioningType;
    }
}