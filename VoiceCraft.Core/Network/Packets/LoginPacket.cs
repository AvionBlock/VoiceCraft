using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class LoginPacket : VoiceCraftPacket
    {
        public LoginPacket(
            Guid userGuid = new Guid(),
            Guid serverUserGuid = new Guid(),
            string locale = "",
            string version = "",
            LoginType loginType = LoginType.Unknown,
            PositioningType positioningType = PositioningType.Server)
        {
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            Version = version;
            LoginType = loginType;
            PositioningType = positioningType;
        }

        public override PacketType PacketType => PacketType.Login;

        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public string Version { get; private set; }
        public LoginType LoginType { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(UserGuid);
            writer.Put(ServerUserGuid);
            writer.Put(Locale, Constants.MaxStringLength);
            writer.Put(Version, Constants.MaxStringLength);
            writer.Put((byte)LoginType);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            UserGuid = reader.GetGuid();
            ServerUserGuid = reader.GetGuid();
            Locale = reader.GetString(Constants.MaxStringLength);
            Version = reader.GetString(Constants.MaxStringLength);
            var loginTypeValue = reader.GetByte();
            LoginType = Enum.IsDefined(typeof(LoginType), loginTypeValue) ? (LoginType)loginTypeValue : LoginType.Unknown;
            var positioningTypeValue = reader.GetByte();
            PositioningType = Enum.IsDefined(typeof(PositioningType), positioningTypeValue) ? (PositioningType)positioningTypeValue : PositioningType.Unknown;
        }
    }
}