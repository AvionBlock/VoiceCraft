using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityRotationRequestPacket : IMcApiPacket
    {
        public McApiSetEntityRotationRequestPacket() : this(string.Empty, 0, Vector2.Zero)
        {
        }
        
        public McApiSetEntityRotationRequestPacket(string token, int id, Vector2 value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityRotationRequest;

        public string Token { get; private set; }
        public int Id { get; private set; }
        public Vector2 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = new Vector2(reader.GetFloat(), reader.GetFloat());
        }
        
        public McApiSetEntityRotationRequestPacket Set(string token = "", int id = 0, Vector2 value = new Vector2())
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}