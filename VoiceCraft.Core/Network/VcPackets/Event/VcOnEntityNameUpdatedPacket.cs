using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Event
{
    public class VcOnEntityNameUpdatedPacket : IVoiceCraftPacket
    {
        public VcOnEntityNameUpdatedPacket(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
        }

        public VcPacketType PacketType => VcPacketType.OnEntityNameUpdated;

        public int Id { get; private set; }
        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value, Constants.MaxStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxStringLength);
        }
        
        public VcOnEntityNameUpdatedPacket Set(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}