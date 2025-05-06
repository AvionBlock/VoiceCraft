using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        
        public int Id { get; private set; }
        public EntityType EntityType { get; private set; }
        public VoiceCraftEntity? Entity { get; private set; }

        public EntityCreatedPacket(int id = 0, VoiceCraftEntity? entity = null)
        {
            Id = id;
            EntityType = entity?.EntityType ?? EntityType.Unknown;
            Entity = entity;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put((byte)EntityType);
            if(Entity != null)
                writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            var entityTypeValue = reader.GetByte();
            EntityType = Enum.IsDefined(typeof(EntityType), entityTypeValue) ? (EntityType)entityTypeValue : EntityType.Unknown;
        }
    }
}