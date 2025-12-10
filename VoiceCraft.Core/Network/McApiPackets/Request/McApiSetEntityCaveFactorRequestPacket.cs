using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityCaveFactorRequestPacket : IMcApiPacket
    {
        public McApiSetEntityCaveFactorRequestPacket() : this(string.Empty, 0, 0.0f)
        {
        }
        
        public McApiSetEntityCaveFactorRequestPacket(string token, int id, float value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityCaveFactorRequest;

        public string Token { get; private set; }
        public int Id { get; private set; }
        public float Value { get; private set; }

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
            Value = reader.GetFloat();
        }
        
        public McApiSetEntityCaveFactorRequestPacket Set(string token = "", int id = 0, float value = 0.0f)
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}