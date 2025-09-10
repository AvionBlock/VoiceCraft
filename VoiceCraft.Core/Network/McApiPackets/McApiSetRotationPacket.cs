using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiSetRotationPacket : McApiPacket
    {
        public McApiSetRotationPacket(string sessionToken = "", int id = 0, Vector2 value = new Vector2())
        {
            SessionToken = sessionToken;
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.SetRotation;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public Vector2 Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = new Vector2(reader.GetFloat(), reader.GetFloat());
        }
    }
}