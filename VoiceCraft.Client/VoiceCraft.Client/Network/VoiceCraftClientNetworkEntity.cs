using System;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientNetworkEntity: VoiceCraftClientEntity
{
    public VoiceCraftClientNetworkEntity(int id, VoiceCraftWorld world) : base(id, world)
    {
        Name = "New Client";
    }

    public override EntityType EntityType => EntityType.Network;

    public Guid UserGuid { get; private set; } = Guid.Empty;

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
}