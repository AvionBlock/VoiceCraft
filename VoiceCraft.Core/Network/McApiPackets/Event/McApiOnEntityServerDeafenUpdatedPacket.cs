using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityServerDeafenUpdatedPacket : IMcApiPacket
    {
        public McApiOnEntityServerDeafenUpdatedPacket() : this(0, false)
        {
        }

        public McApiOnEntityServerDeafenUpdatedPacket(int id, bool value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.OnEntityServerDeafenUpdated;

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

        public McApiOnEntityServerDeafenUpdatedPacket Set(int id = 0, bool value = false)
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}