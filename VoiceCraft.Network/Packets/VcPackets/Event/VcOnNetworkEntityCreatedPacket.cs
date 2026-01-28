using System;
using LiteNetLib.Utils;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Packets.VcPackets.Event;

public class VcOnNetworkEntityCreatedPacket : VcOnEntityCreatedPacket
{
    public VcOnNetworkEntityCreatedPacket() : this(0, string.Empty, false, false, Guid.Empty, false, false)
    {
    }

    public VcOnNetworkEntityCreatedPacket(int id, string name, bool muted, bool deafened, Guid userGuid,
        bool serverMuted, bool serverDeafened) :
        base(id, name, muted, deafened)
    {
        UserGuid = userGuid;
        ServerMuted = serverMuted;
        ServerDeafened = serverDeafened;
    }

    public VcOnNetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
    {
        UserGuid = entity.UserGuid;
        ServerMuted = entity.ServerMuted;
        ServerDeafened = entity.ServerDeafened;
    }

    public override VcPacketType PacketType => VcPacketType.OnNetworkEntityCreated;

    public Guid UserGuid { get; private set; }
    public bool ServerMuted { get; private set; }
    public bool ServerDeafened { get; private set; }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(UserGuid);
        writer.Put(ServerMuted);
        writer.Put(ServerDeafened);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        UserGuid = reader.GetGuid();
        ServerMuted = reader.GetBool();
        ServerDeafened = reader.GetBool();
    }

    public VcOnNetworkEntityCreatedPacket Set(int id = 0, string name = "", bool muted = false,
        bool deafened = false, Guid userGuid = new(), bool serverMute = false, bool serverDeafen = false)
    {
        base.Set(id, name, muted, deafened);
        UserGuid = userGuid;
        ServerMuted = serverMute;
        ServerDeafened = serverDeafen;
        return this;
    }

    public VcOnNetworkEntityCreatedPacket Set(VoiceCraftNetworkEntity entity)
    {
        base.Set(entity);
        UserGuid = entity.UserGuid;
        ServerMuted = entity.ServerMuted;
        ServerDeafened = entity.ServerDeafened;
        return this;
    }
}