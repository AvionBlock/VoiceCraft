using System;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.Packets
{
    public class NetworkEntityCreatedPacket : EntityCreatedPacket
    {
        public NetworkEntityCreatedPacket(int id = 0, string name = "", bool muted = false, bool deafened = false,
            Guid userGuid = new Guid()) :
            base(id, name, muted, deafened)
        {
            UserGuid = userGuid;
        }

        public NetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
        {
            UserGuid = entity.UserGuid;
        }

        public override PacketType PacketType => PacketType.NetworkEntityCreated;

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
    }
}