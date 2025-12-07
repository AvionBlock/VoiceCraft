using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnDeafenUpdatedPacket : IMcApiPacket
    {
        public McApiOnDeafenUpdatedPacket(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityDeafenUpdated;

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

        public void Set(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
        }
    }
}