using System;
using System.Numerics;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.McApiPackets
{
    public class McApiEntityCreatedPacket : McApiPacket
    {
        public McApiEntityCreatedPacket(
            string sessionToken = "",
            int id = 0,
            EntityType entityType = EntityType.Unknown,
            DateTime lastSpoke = new DateTime(),
            bool destroyed = false,
            string worldId = "",
            string name = "",
            bool muted = false,
            bool deafened = false,
            ulong talkBitmask = ulong.MinValue,
            ulong listenBitmask = ulong.MinValue,
            Vector3 position = new Vector3(),
            Quaternion rotation = new Quaternion(),
            Guid? userGuid = null,
            Guid? serverUserGuid = null,
            string? locale = null,
            PositioningType? positioningType = null)
        {
            SessionToken = sessionToken;
            Id = id;
            EntityType = entityType;
            LastSpoke = lastSpoke;
            Destroyed = destroyed;
            WorldId = worldId;
            Name = name;
            Muted = muted;
            Deafened = deafened;
            TalkBitmask = talkBitmask;
            ListenBitmask = listenBitmask;
            Position = position;
            Rotation = rotation;
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            PositioningType = positioningType;
        }

        public override McApiPacketType PacketType => McApiPacketType.EntityCreated;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public EntityType EntityType { get; private set; }
        public DateTime LastSpoke { get; private set; }
        public bool Destroyed { get; private set; }
        public string WorldId { get; private set; }
        public string Name { get; private set; }
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }
        public ulong TalkBitmask { get; private set; }
        public ulong ListenBitmask { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Guid? UserGuid { get; private set; }
        public Guid? ServerUserGuid { get; private set; }
        public string? Locale { get; private set; }
        public PositioningType? PositioningType { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
            writer.Put((byte)EntityType);
            writer.Put((LastSpoke - DateTime.UnixEpoch).TotalMilliseconds);
            writer.Put(Destroyed);
            writer.Put(WorldId, Constants.MaxStringLength);
            writer.Put(Name, Constants.MaxStringLength);
            writer.Put(Muted);
            writer.Put(Deafened);
            writer.Put(TalkBitmask);
            writer.Put(ListenBitmask);
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(Rotation.Z);
            writer.Put(Rotation.W);

            if (EntityType != EntityType.Network) return;
            if (UserGuid == null || ServerUserGuid == null || Locale == null || PositioningType == null)
                throw new InvalidOperationException();

            writer.Put(UserGuid.Value.ToString(), Constants.MaxStringLength);
            writer.Put(ServerUserGuid.Value.ToString(), Constants.MaxStringLength);
            writer.Put(Locale);
            writer.Put((byte)PositioningType.Value);
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            var entityTypeValue = reader.GetByte();
            EntityType = Enum.IsDefined(typeof(EntityType), entityTypeValue)
                ? (EntityType)entityTypeValue
                : EntityType.Unknown;
            LastSpoke = DateTime.FromOADate(reader.GetDouble());
            Destroyed = reader.GetBool();
            WorldId = reader.GetString(Constants.MaxStringLength);
            Name = reader.GetString(Constants.MaxStringLength);
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
            TalkBitmask = reader.GetULong();
            ListenBitmask = reader.GetULong();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

            if (EntityType != EntityType.Network) return;
            UserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
            ServerUserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
            Locale = reader.GetString(Constants.MaxStringLength);
            var positioningTypeValue = reader.GetByte();
            PositioningType = Enum.IsDefined(typeof(PositioningType), positioningTypeValue)
                ? (PositioningType)entityTypeValue
                : Core.PositioningType.Unknown;
        }
    }
}