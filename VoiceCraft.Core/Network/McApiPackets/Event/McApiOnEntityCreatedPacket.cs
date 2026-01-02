using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Network.McApiPackets.Event
{
    public class McApiOnEntityCreatedPacket : IMcApiPacket
    {
        public McApiOnEntityCreatedPacket() : this(
            0,
            0.0f,
            DateTime.MinValue,
            string.Empty,
            string.Empty,
            false,
            false,
            0,
            0,
            0,
            Vector3.Zero,
            Vector2.Zero,
            0.0f,
            0.0f)
        {
        }

        public McApiOnEntityCreatedPacket(
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
            float muffleFactor)
        {
            Id = id;
            Loudness = loudness;
            LastSpoke = lastSpoke;
            WorldId = worldId;
            Name = name;
            Muted = muted;
            Deafened = deafened;
            TalkBitmask = talkBitmask;
            ListenBitmask = listenBitmask;
            EffectBitmask = effectBitmask;
            Position = position;
            Rotation = rotation;
            CaveFactor = caveFactor;
            MuffleFactor = muffleFactor;
        }

        public McApiOnEntityCreatedPacket(VoiceCraftEntity entity)
        {
            Id = entity.Id;
            Loudness = entity.Loudness;
            LastSpoke = entity.LastSpoke;
            WorldId = entity.WorldId;
            Name = entity.Name;
            Muted = entity.Muted;
            Deafened = entity.Deafened;
            TalkBitmask = entity.TalkBitmask;
            ListenBitmask = entity.ListenBitmask;
            EffectBitmask = entity.EffectBitmask;
            Position = entity.Position;
            Rotation = entity.Rotation;
            CaveFactor = entity.CaveFactor;
            MuffleFactor = entity.MuffleFactor;
        }

        public virtual McApiPacketType PacketType => McApiPacketType.OnEntityCreated;

        public int Id { get; private set; }
        public float Loudness { get; private set; }
        public DateTime LastSpoke { get; private set; }
        public string WorldId { get; private set; }
        public string Name { get; private set; }
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }
        public ushort TalkBitmask { get; private set; }
        public ushort ListenBitmask { get; private set; }
        public ushort EffectBitmask { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector2 Rotation { get; private set; }
        public float CaveFactor { get; private set; }
        public float MuffleFactor { get; private set; }

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.Put(Loudness);
            writer.Put(LastSpoke.Ticks);
            writer.Put(WorldId, Constants.MaxStringLength);
            writer.Put(Name, Constants.MaxStringLength);
            writer.Put(Muted);
            writer.Put(Deafened);
            writer.Put(TalkBitmask);
            writer.Put(ListenBitmask);
            writer.Put(EffectBitmask);
            writer.Put(Position.X);
            writer.Put(Position.Y);
            writer.Put(Position.Z);
            writer.Put(Rotation.X);
            writer.Put(Rotation.Y);
            writer.Put(CaveFactor);
            writer.Put(MuffleFactor);
        }

        public virtual void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Loudness = reader.GetFloat();
            LastSpoke = new DateTime(reader.GetLong());
            WorldId = reader.GetString(Constants.MaxStringLength);
            Name = reader.GetString(Constants.MaxStringLength);
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
            TalkBitmask = reader.GetUShort();
            ListenBitmask = reader.GetUShort();
            EffectBitmask = reader.GetUShort();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Vector2(reader.GetFloat(), reader.GetFloat());
            CaveFactor = reader.GetFloat();
            MuffleFactor = reader.GetFloat();
        }

        public McApiOnEntityCreatedPacket Set(
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
            float muffleFactor = 0)
        {
            Id = id;
            Loudness = loudness;
            LastSpoke = lastSpoke;
            WorldId = worldId;
            Name = name;
            Muted = muted;
            Deafened = deafened;
            TalkBitmask = talkBitmask;
            ListenBitmask = listenBitmask;
            EffectBitmask = effectBitmask;
            Position = position;
            Rotation = rotation;
            CaveFactor = caveFactor;
            MuffleFactor = muffleFactor;
            return this;
        }

        public McApiOnEntityCreatedPacket Set(VoiceCraftEntity entity)
        {
            Id = entity.Id;
            Loudness = entity.Loudness;
            LastSpoke = entity.LastSpoke;
            WorldId = entity.WorldId;
            Name = entity.Name;
            Muted = entity.Muted;
            Deafened = entity.Deafened;
            TalkBitmask = entity.TalkBitmask;
            ListenBitmask = entity.ListenBitmask;
            EffectBitmask = entity.EffectBitmask;
            Position = entity.Position;
            Rotation = entity.Rotation;
            CaveFactor = entity.CaveFactor;
            MuffleFactor = entity.MuffleFactor;
            return this;
        }
    }
}