using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiSetRotationPacket : McApiPacket
    {
        public McApiSetRotationPacket(string sessionToken = "", int id = 0, Quaternion value = new Quaternion())
        {
            SessionToken = sessionToken;
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.SetRotation;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public Quaternion Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put(Value.X);
            writer.Put(Value.Y);
            writer.Put(Value.Z);
            writer.Put(Value.W);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            Value = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }
}