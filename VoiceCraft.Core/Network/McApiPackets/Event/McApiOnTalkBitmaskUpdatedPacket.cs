using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnTalkBitmaskUpdatedPacket : IMcApiPacket
    {
        public McApiOnTalkBitmaskUpdatedPacket(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityTalkBitmaskUpdated;

        public int Id { get; private set; }
        public ushort Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetUShort();
        }
        
        public void Set(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
        }
    }
}