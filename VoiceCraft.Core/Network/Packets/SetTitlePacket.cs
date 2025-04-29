using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTitlePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetTitle;
        
        public string Value { get; private set; }

        public SetTitlePacket(string value = "")
        {
            Value = value;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Value, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Value = reader.GetString(Constants.MaxStringLength);
        }
    }
}