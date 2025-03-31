using System.ComponentModel.DataAnnotations;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LoginPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Login;
        [StringLength(Constants.MaxStringLength)]
        public string Version { get; private set; }
        public LoginType LoginType { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public LoginPacket(string version, LoginType loginType, PositioningType positioningType)
        {
            Version = version;
            LoginType = loginType;
            PositioningType = positioningType;
        }
        
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