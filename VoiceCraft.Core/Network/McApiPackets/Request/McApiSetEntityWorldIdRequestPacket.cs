using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityWorldIdRequestPacket : IMcApiPacket
    {
        public McApiSetEntityWorldIdRequestPacket() : this(string.Empty, 0, string.Empty)
        {
        }
        
        public McApiSetEntityWorldIdRequestPacket(string token, int id, string value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityWorldIdRequest;

        public string Token { get; private set; }
        public int Id { get; private set; }
        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxStringLength);
        }
        
        public McApiSetEntityWorldIdRequestPacket Set(string token = "", int id = 0, string value = "")
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}