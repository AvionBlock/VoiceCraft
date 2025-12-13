using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityEffectBitmaskRequestPacket : IMcApiPacket
    {
        public McApiSetEntityEffectBitmaskRequestPacket() : this(0, 0)
        {
        }
        
        public McApiSetEntityEffectBitmaskRequestPacket(int id, ushort value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityEffectBitmaskRequest;
        
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
        
        public McApiSetEntityEffectBitmaskRequestPacket Set(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}