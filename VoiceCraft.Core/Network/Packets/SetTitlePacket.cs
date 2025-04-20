using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTitlePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetTitle;
        public string Title { get; private set; }

        public SetTitlePacket(string title = "")
        {
            Title = title;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Title, Constants.MaxStringLength);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Title = reader.GetString(Constants.MaxStringLength);
        }
    }
}