using System.Numerics;
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data;

public class VoiceCraftNetworkEntity : VoiceCraftEntity
{
    public VoiceCraftNetworkEntity(
        NetPeer netPeer,
        Guid userGuid,
        string locale,
        PositioningType positioningType,
        VoiceCraftWorld world) : base(netPeer.Id, world)
    {
        Name = "New Client";
        NetPeer = netPeer;
        UserGuid = userGuid;
        Locale = locale;
        PositioningType = positioningType;
        AddVisibleEntity(this); //Should always be visible to itself.
    }

    public NetPeer NetPeer { get; }
    public Guid UserGuid { get; private set; }
    public string Locale { get; private set; }
    public PositioningType PositioningType { get; }
    public override EntityType EntityType => EntityType.Network;

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(UserGuid);
        base.Serialize(writer);
    }

    public override void Deserialize(NetDataReader reader)
    {
        var userGuid = reader.GetGuid();
        base.Deserialize(reader);
        UserGuid = userGuid;
    }

    public override void Reset()
    {
        //Doesn't remove the entity from the world.
        Name = "New Client";
        WorldId = string.Empty;
        Position = Vector3.Zero;
        Rotation = Quaternion.Zero;
        TalkBitmask = ulong.MaxValue;
        ListenBitmask = ulong.MaxValue;
        ClearProperties();
    }
}