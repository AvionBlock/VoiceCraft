using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetDescriptionPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetDescription;
        public string Description { get; private set; }

        public SetDescriptionPacket(string description = "")
        {
            Description = description;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Description, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Description = reader.GetString(Constants.MaxStringLength);
        }
    }
}