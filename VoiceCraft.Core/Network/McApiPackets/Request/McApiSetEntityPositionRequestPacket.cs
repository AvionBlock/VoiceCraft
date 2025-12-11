using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityPositionRequestPacket : IMcApiPacket
    {
        public McApiSetEntityPositionRequestPacket() : this(string.Empty, 0, Vector3.Zero)
        {
        }
        
        public McApiSetEntityPositionRequestPacket(string token, int id, Vector3 value)
        {
            Token = token;
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityPositionRequest;

        public string Token { get; private set; }
        public int Id { get; private set; }
        public Vector3 Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Token, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
        }

        public void Deserialize(NetDataReader reader)
        {
            Token = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
        
        public McApiSetEntityPositionRequestPacket Set(string token = "", int id = 0, Vector3 value = new Vector3())
        {
            Token = token;
            Id = id;
            Value = value;
            return this;
        }
    }
}