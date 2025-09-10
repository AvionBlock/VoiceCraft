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
            DateTime lastSpoke = new DateTime(),
            bool destroyed = false,
            string worldId = "",
            string name = "",
            bool muted = false,
            bool deafened = false,
            uint talkBitmask = uint.MinValue,
            uint listenBitmask = uint.MinValue,
            Vector3 position = new Vector3(),
            Vector2 rotation = new Vector2())
        {
            SessionToken = sessionToken;
            Id = id;
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
        }

        public override McApiPacketType PacketType => McApiPacketType.EntityCreated;

        public string SessionToken { get; private set; }
        public int Id { get; private set; }
        public DateTime LastSpoke { get; private set; }
        public bool Destroyed { get; private set; }
        public string WorldId { get; private set; }
        public string Name { get; private set; }
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }
        public uint TalkBitmask { get; private set; }
        public uint ListenBitmask { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector2 Rotation { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(SessionToken, Constants.MaxStringLength);
            writer.Put(Id);
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
        }

        public override void Deserialize(NetDataReader reader)
        {
            SessionToken = reader.GetString(Constants.MaxStringLength);
            Id = reader.GetInt();
            LastSpoke = DateTime.FromOADate(reader.GetDouble());
            Destroyed = reader.GetBool();
            WorldId = reader.GetString(Constants.MaxStringLength);
            Name = reader.GetString(Constants.MaxStringLength);
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
            TalkBitmask = reader.GetUInt();
            ListenBitmask = reader.GetUInt();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Vector2(reader.GetFloat(), reader.GetFloat());
        }
    }
}