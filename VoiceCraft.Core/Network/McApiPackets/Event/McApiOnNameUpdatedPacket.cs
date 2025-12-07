using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnNameUpdatedPacket : IMcApiPacket
    {
        public McApiOnNameUpdatedPacket(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityNameUpdated;

        public int Id { get; private set; }
        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxStringLength);
        }
        
        public void Set(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
        }
    }
}