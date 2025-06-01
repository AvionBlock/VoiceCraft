using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetDescriptionPacket : VoiceCraftPacket
    {
        public SetDescriptionPacket(string value = "")
        {
            Value = value;
        }

        public override PacketType PacketType => PacketType.SetDescription;

        public string Value { get; private set; }

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