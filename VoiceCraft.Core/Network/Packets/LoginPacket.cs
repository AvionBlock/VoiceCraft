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
            Version? version = null,
            PositioningType positioningType = PositioningType.Server)
        {
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            Version = version ?? new Version(0, 0, 0);
            PositioningType = positioningType;
        }

        public override PacketType PacketType => PacketType.Login;

        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public Version Version { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(UserGuid);
            writer.Put(ServerUserGuid);
            writer.Put(Locale, Constants.MaxStringLength);
            writer.Put(Version.Major);
            writer.Put(Version.Minor);
            writer.Put(Version.Build);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            UserGuid = reader.GetGuid();
            ServerUserGuid = reader.GetGuid();
            Locale = reader.GetString(Constants.MaxStringLength);
            Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
            PositioningType = (PositioningType)reader.GetByte();
        }
    }
}