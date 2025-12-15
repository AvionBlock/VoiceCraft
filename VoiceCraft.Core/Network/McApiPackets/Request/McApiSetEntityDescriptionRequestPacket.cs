using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityDescriptionRequestPacket : IMcApiPacket
    {
        public McApiSetEntityDescriptionRequestPacket() : this(0, string.Empty)
        {
        }

        public McApiSetEntityDescriptionRequestPacket(int id, string value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityDescriptionRequest;
        
        public int Id { get; private set; }
        public string Value { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Value, Constants.MaxDescriptionStringLength);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Value = reader.GetString(Constants.MaxDescriptionStringLength);
        }

        public McApiSetEntityDescriptionRequestPacket Set(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}