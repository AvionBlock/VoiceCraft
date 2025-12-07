using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityMuteUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityMuteUpdatedPacket() : this(0, false)
        {
        }

        public VcOnEntityMuteUpdatedPacket(int id, bool value)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityMuteUpdated;

        public int Id { get; private set; }
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetBool();
        }

        public VcOnEntityMuteUpdatedPacket Set(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}