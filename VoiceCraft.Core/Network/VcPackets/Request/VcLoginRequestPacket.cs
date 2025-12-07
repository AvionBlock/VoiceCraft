using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.VcPackets.Request
{
    public class VcLoginRequestPacket : IVoiceCraftPacket
    {
        public VcLoginRequestPacket(
            Guid requestId = new Guid(),
            Guid userGuid = new Guid(),
            Guid serverUserGuid = new Guid(),
            string locale = "",
            Version? version = null,
            PositioningType positioningType = PositioningType.Server)
        {
            RequestId = requestId;
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            Version = version ?? new Version(0, 0, 0);
            PositioningType = positioningType;
        }

        public VcPacketType PacketType => VcPacketType.LoginRequest;

        public Guid RequestId { get; private set; }
        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public Version Version { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RequestId);
            writer.Put(UserGuid);
            writer.Put(ServerUserGuid);
            writer.Put(Locale, Constants.MaxStringLength);
            writer.Put(Version.Major);
            writer.Put(Version.Minor);
            writer.Put(Version.Build);
            writer.Put((byte)PositioningType);
        }

        public void Deserialize(NetDataReader reader)
        {
            RequestId = reader.GetGuid();
            UserGuid = reader.GetGuid();
            ServerUserGuid = reader.GetGuid();
            Locale = reader.GetString(Constants.MaxStringLength);
            Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
            PositioningType = (PositioningType)reader.GetByte();
        }

        public VcLoginRequestPacket Set(
            Guid requestId = new Guid(),
            Guid userGuid = new Guid(),
            Guid serverUserGuid = new Guid(),
            string locale = "",
            Version? version = null,
            PositioningType positioningType = PositioningType.Server)
        {
            RequestId = requestId;
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            Version = version ?? new Version(0, 0, 0);
            PositioningType = positioningType;
            return this;
        }
    }
}