using System;
using LiteNetLib.Utils;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnNetworkEntityCreatedPacket : VcOnEntityCreatedPacket
{
    public VcOnNetworkEntityCreatedPacket() : this(0, Guid.Empty)
    {
    }

    public VcOnNetworkEntityCreatedPacket(int id, Guid userGuid) : base(id)
    {
        UserGuid = userGuid;
    }

    public VcOnNetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
    {
        UserGuid = entity.UserGuid;
    }

    public override EventType EventType => EventType.OnNetworkEntityCreated;
    public Guid UserGuid { get; private set; }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(UserGuid);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        UserGuid = reader.GetGuid();
    }
    
    public override void Return()
    {
        PacketPool<VcOnNetworkEntityCreatedPacket>.Return(this);
    }

    public VcOnNetworkEntityCreatedPacket Set(int id = 0, Guid userGuid = new())
    {
        base.Set(id);
        UserGuid = userGuid;
        return this;
    }

    public VcOnNetworkEntityCreatedPacket Set(VoiceCraftNetworkEntity entity)
    {
        base.Set(entity);
        UserGuid = entity.UserGuid;
        return this;
    }
}