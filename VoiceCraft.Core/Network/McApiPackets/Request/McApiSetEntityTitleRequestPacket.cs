using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets.Request
{
    public class McApiSetEntityTitleRequestPacket : IMcApiPacket
    {
        public McApiSetEntityTitleRequestPacket() : this(0, string.Empty)
        {
        }
        
        public McApiSetEntityTitleRequestPacket(int id, string value)
        {
            Id = id;
            Value = value;
        }

        public McApiPacketType PacketType => McApiPacketType.SetEntityTitleRequest;
        
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
        
        public McApiSetEntityTitleRequestPacket Set(int id = 0, string value = "")
        {
            Id = id;
            Value = value;
            return this;
        }
    }
}