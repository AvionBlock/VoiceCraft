using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnListenBitmaskUpdatedPacket : McApiPacket
    {
        public McApiOnListenBitmaskUpdatedPacket(int id = 0, ushort value = 0)
        {
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnEntityListenBitmaskUpdated;

        public int Id { get; private set; }
        public ushort Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetUShort();
        }
    }
}