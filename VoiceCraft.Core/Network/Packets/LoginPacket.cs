using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LoginPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Login;
        public string Version { get; private set; }
        public LoginType LoginType { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public LoginPacket(string version = "", LoginType loginType = LoginType.Unknown, PositioningType positioningType = PositioningType.Server)
        {
            Version = version;
            LoginType = loginType;
            PositioningType = positioningType;
        }
        
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(Version, Constants.MaxStringLength);
            writer.Put((byte)LoginType);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Version = reader.GetString(Constants.MaxStringLength);
            var loginTypeValue = reader.GetByte();
            LoginType = Enum.IsDefined(typeof(LoginType), loginTypeValue) ? (LoginType)loginTypeValue : LoginType.Unknown;
            var positioningTypeValue = reader.GetByte();
            PositioningType = Enum.IsDefined(typeof(PositioningType), positioningTypeValue) ? (PositioningType)positioningTypeValue : PositioningType.Unknown;
        }
    }
}