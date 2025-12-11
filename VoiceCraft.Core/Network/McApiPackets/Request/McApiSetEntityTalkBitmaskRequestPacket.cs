using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityTalkBitmaskRequestPacket : IMcApiPacket
    {
        public McApiSetEntityTalkBitmaskRequestPacket() : this(string.Empty, 0, 0)
        {
        }
        
        public McApiSetEntityTalkBitmaskRequestPacket(string token, int id, ushort value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityTalkBitmaskRequest;

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
        
        public McApiSetEntityTalkBitmaskRequestPacket Set(string token = "", int id = 0, ushort value = 0)
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}