using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityDeafenUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityDeafenUpdatedPacket(int id = 0, bool value = true)
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityDeafenUpdated;

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
        
        public VcOnEntityDeafenUpdatedPacket Set(int id = 0, bool value = true)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}