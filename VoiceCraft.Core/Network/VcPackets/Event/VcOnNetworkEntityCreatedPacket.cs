using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnNetworkEntityCreatedPacket : VcOnEntityCreatedPacket
    {
        public VcOnNetworkEntityCreatedPacket(int id = 0, string name = "", bool muted = false, bool deafened = false,
            Guid userGuid = new Guid()) :
            base(id, name, muted, deafened)
        {
            UserGuid = userGuid;
        }

        public VcOnNetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
        {
            UserGuid = entity.UserGuid;
        }

        public override VcPacketType PacketType => VcPacketType.OnNetworkEntityCreated;

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

        public void Set(int id = 0, string name = "", bool muted = false, bool deafened = false,
            Guid userGuid = new Guid())
        {
            base.Set(id, name, muted, deafened);
            UserGuid = userGuid;
        }

        public VcOnNetworkEntityCreatedPacket Set(VoiceCraftNetworkEntity entity)
        {
            UserGuid = entity.UserGuid;
            return this;
        }
    }
}