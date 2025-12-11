using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityListenBitmaskRequestPacket : IMcApiPacket
    {
        public McApiSetEntityListenBitmaskRequestPacket() : this(string.Empty, 0, 0)
        {
        }
        
        public McApiSetEntityListenBitmaskRequestPacket(string token, int id, ushort value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityListenBitmaskRequest;

        public string Token { get; private set; }
        public int Id { get; private set; }
        public ushort Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = reader.GetUShort();
        }
        
        public McApiSetEntityListenBitmaskRequestPacket Set(string token = "", int id = 0, ushort value = 0)
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}