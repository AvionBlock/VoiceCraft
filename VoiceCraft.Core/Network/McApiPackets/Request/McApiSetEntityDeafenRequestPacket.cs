using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityDeafenRequestPacket : IMcApiPacket
    {
        public McApiSetEntityDeafenRequestPacket() : this(0, false)
        {
        }

        public McApiSetEntityDeafenRequestPacket(int id, bool value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityDeafenRequest;

        public int Id { get; private set; }
        public bool Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetBool();
        }

        public McApiSetEntityDeafenRequestPacket Set(int id = 0, bool value = true)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}