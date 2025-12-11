using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityNameRequestPacket : IMcApiPacket
    {
        public McApiSetEntityNameRequestPacket() : this(string.Empty, 0, string.Empty)
        {
        }
        
        public McApiSetEntityNameRequestPacket(string token, int id, string value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityNameRequest;

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
        
        public McApiSetEntityNameRequestPacket Set(string token = "", int id = 0, string value = "")
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}