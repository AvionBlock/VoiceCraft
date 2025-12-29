using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityServerDeafenUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityServerDeafenUpdatedPacket() : this(0, false)
        {
        }

        public VcOnEntityServerDeafenUpdatedPacket(int id, bool value)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityServerDeafenUpdated;
        
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

        public VcOnEntityServerDeafenUpdatedPacket Set(int id = 0, bool value = true)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}