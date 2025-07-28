using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiEntityCreatedPacket : McApiPacket
    {
        public McApiEntityCreatedPacket(string sessionToken = "", int id = 0, VoiceCraftEntity? entity = null)
        {
            SessionToken = sessionToken;
            Id = id;
            EntityType = entity?.EntityType ?? EntityType.Unknown;
            Entity = entity;
        }

        public override McApiPacketType PacketType => McApiPacketType.EntityCreated;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public EntityType EntityType { get; private set; }
        public VoiceCraftEntity? Entity { get; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put((byte)EntityType);
            if (Entity != null)
                writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            var entityTypeValue = reader.GetByte();
            EntityType = Enum.IsDefined(typeof(EntityType), entityTypeValue) ? (EntityType)entityTypeValue : EntityType.Unknown;
        }
    }
}