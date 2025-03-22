using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LoginPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Login;
        [StringLength(Constants.MaxStringLength)]
        public string Version { get; set; } = string.Empty;
        public LoginType LoginType { get; set; }
        public PositioningType PositioningType { get; set; }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Version);
            writer.Put((byte)LoginType);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Version = reader.GetString();
            LoginType = (LoginType)reader.GetByte();
            PositioningType = (PositioningType)reader.GetByte();
        }
    }
}