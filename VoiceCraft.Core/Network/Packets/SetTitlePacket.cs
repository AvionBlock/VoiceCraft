using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class SetTitlePacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.SetTitle;
        [StringLength(Constants.MaxStringLength)]
        public string Title { get; private set; }

        public SetTitlePacket(string title = "")
        {
            Title = title;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Title);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Title = reader.GetString();
        }
    }
}