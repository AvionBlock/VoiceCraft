using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityTalkBitmaskRequestPacket : IMcApiPacket
    {
        public McApiSetEntityTalkBitmaskRequestPacket() : this(0, 0)
        {
        }
        
        public McApiSetEntityTalkBitmaskRequestPacket(int id, ushort value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityTalkBitmaskRequest;
        
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
        
        public McApiSetEntityTalkBitmaskRequestPacket Set(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}