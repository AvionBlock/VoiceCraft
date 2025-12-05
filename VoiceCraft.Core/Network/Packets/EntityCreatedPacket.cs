using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityCreatedPacket : VoiceCraftPacket
    {
        public EntityCreatedPacket(int id = 0, string name = "", bool muted = false, bool deafened = false)
        {
            Id = id;
            Name = name;
            Muted = muted;
            Deafened = deafened;
        }

        public EntityCreatedPacket(VoiceCraftEntity entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            Muted = entity.Muted;
            Deafened = entity.Deafened;
        }

        public override PacketType PacketType => PacketType.EntityCreated;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Name, Constants.MaxStringLength);
            writer.Put(Muted);
            writer.Put(Deafened);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Name = reader.GetString(Constants.MaxStringLength);
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
        }
    }
}