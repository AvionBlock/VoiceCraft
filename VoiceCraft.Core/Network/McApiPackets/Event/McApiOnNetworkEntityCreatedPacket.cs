using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnNetworkEntityCreatedPacket : McApiOnEntityCreatedPacket
    {
        public McApiOnNetworkEntityCreatedPacket() : this(0, 0.0f, DateTime.MinValue, string.Empty, string.Empty, false,
            false, 0, 0, 0, Vector3.Zero, Vector2.Zero, 0.0f, 0.0f, Guid.Empty, Guid.Empty, string.Empty,
            PositioningType.Server)
        {
        }

        public McApiOnNetworkEntityCreatedPacket(
            int id,
            float loudness,
            DateTime lastSpoke,
            string worldId,
            string name,
            bool muted,
            bool deafened,
            ushort talkBitmask,
            ushort listenBitmask,
            ushort effectBitmask,
            Vector3 position,
            Vector2 rotation,
            float caveFactor,
            float muffleFactor,
            Guid userGuid,
            Guid serverUserGuid,
            string locale,
            PositioningType positioningType) :
            base(id,
                loudness,
                lastSpoke,
                worldId,
                name,
                muted,
                deafened,
                talkBitmask,
                listenBitmask,
                effectBitmask,
                position,
                rotation,
                caveFactor,
                muffleFactor)
        {
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            PositioningType = positioningType;
        }

        public McApiOnNetworkEntityCreatedPacket(VoiceCraftNetworkEntity entity) : base(entity)
        {
            UserGuid = entity.UserGuid;
            ServerUserGuid = entity.ServerUserGuid;
            Locale = entity.Locale;
            PositioningType = entity.PositioningType;
        }

        public override McApiPacketType PacketType => McApiPacketType.OnNetworkEntityCreated;

        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public PositioningType PositioningType { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(UserGuid.ToString(), Constants.MaxStringLength);
            writer.Put(ServerUserGuid.ToString(), Constants.MaxStringLength);
            writer.Put(Locale, Constants.MaxStringLength);
            writer.Put((byte)PositioningType);
        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            UserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
            ServerUserGuid = Guid.Parse(reader.GetString(Constants.MaxStringLength));
            Locale = reader.GetString(Constants.MaxStringLength);
            PositioningType = (PositioningType)reader.GetByte();
        }

        public McApiOnNetworkEntityCreatedPacket Set(
            int id = 0,
            float loudness = 0.0f,
            DateTime lastSpoke = new DateTime(),
            string worldId = "",
            string name = "",
            bool muted = false,
            bool deafened = false,
            ushort talkBitmask = 0,
            ushort listenBitmask = 0,
            ushort effectBitmask = 0,
            Vector3 position = new Vector3(),
            Vector2 rotation = new Vector2(),
            float caveFactor = 0,
            float muffleFactor = 0,
            Guid userGuid = new Guid(),
            Guid serverUserGuid = new Guid(),
            string locale = "",
            PositioningType positioningType = PositioningType.Server)
        {
            base.Set(id, loudness, lastSpoke, worldId, name, muted, deafened, talkBitmask, listenBitmask, effectBitmask,
                position, rotation, caveFactor, muffleFactor);
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            PositioningType = positioningType;
            return this;
        }

        public McApiOnNetworkEntityCreatedPacket Set(VoiceCraftNetworkEntity entity)
        {
            base.Set(entity);
            UserGuid = entity.UserGuid;
            ServerUserGuid = entity.ServerUserGuid;
            Locale = entity.Locale;
            PositioningType = entity.PositioningType;
            return this;
        }
    }
}