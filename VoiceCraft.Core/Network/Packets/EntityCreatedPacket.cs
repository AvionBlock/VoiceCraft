using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityCreated;
        public int NetworkId { get; private set; }
        public VoiceCraftEntity Entity { get; private set; }

        public EntityCreatedPacket(int networkId = 0, VoiceCraftEntity? entity = null)
        {
            NetworkId = networkId;
            Entity = entity ?? new VoiceCraftEntity(networkId);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Entity.Id);
            writer.Put(Entity);
        }

        public override void Deserialize(NetDataReader reader)
        {
            NetworkId = reader.GetInt();
            Entity = new VoiceCraftEntity(NetworkId);
            Entity.Deserialize(reader);
        }
    }
}