using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityCreatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityCreatedPacket() : this(0, string.Empty, false, false)
        {
        }

        public VcOnEntityCreatedPacket(int id, string name, bool muted, bool deafened)
        {
            Id = id;
            Name = name;
            Muted = muted;
            Deafened = deafened;
        }

        public VcOnEntityCreatedPacket(VoiceCraftEntity entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            Muted = entity.Muted;
            Deafened = entity.Deafened;
        }

        public virtual VcPacketType PacketType => VcPacketType.OnEntityCreated;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Name, Constants.MaxStringLength);
            writer.Put(Muted);
            writer.Put(Deafened);
        }

        public virtual void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Name = reader.GetString(Constants.MaxStringLength);
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
        }

        public void Set(int id = 0, string name = "", bool muted = false, bool deafened = false)
        {
            Id = id;
            Name = name;
            Muted = muted;
            Deafened = deafened;
        }

        public VcOnEntityCreatedPacket Set(VoiceCraftEntity entity)
        {
            Id = entity.Id;
            Name = entity.Name;
            Muted = entity.Muted;
            Deafened = entity.Deafened;
            return this;
        }
    }
}