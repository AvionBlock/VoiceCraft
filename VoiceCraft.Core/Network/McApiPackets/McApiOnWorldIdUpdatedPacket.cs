using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiOnWorldIdUpdatedPacket : McApiPacket
    {
        public McApiOnWorldIdUpdatedPacket(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnEntityWorldIdUpdated;

        public int Id { get; private set; }
        public string Value { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxStringLength);
        }
    }
}