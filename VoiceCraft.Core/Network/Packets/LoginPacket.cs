using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LoginPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.Login;
        public Guid UserGuid { get; private set; }
        public string Version { get; private set; }
        public LoginType LoginType { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public LoginPacket(Guid userGuid = new Guid(), string version = "", LoginType loginType = LoginType.Unknown,
            PositioningType positioningType = PositioningType.Server)
        {
            UserGuid = userGuid;
            Version = version;
            LoginType = loginType;
            PositioningType = positioningType;
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(UserGuid);
            writer.Put(Version, Constants.MaxStringLength);
            writer.Put((byte)LoginType);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            UserGuid = reader.GetGuid();
            Version = reader.GetString(Constants.MaxStringLength);
            var loginTypeValue = reader.GetByte();
            LoginType = Enum.IsDefined(typeof(LoginType), loginTypeValue) ? (LoginType)loginTypeValue : LoginType.Unknown;
            var positioningTypeValue = reader.GetByte();
            PositioningType = Enum.IsDefined(typeof(PositioningType), positioningTypeValue) ? (PositioningType)positioningTypeValue : PositioningType.Unknown;
        }
    }
}