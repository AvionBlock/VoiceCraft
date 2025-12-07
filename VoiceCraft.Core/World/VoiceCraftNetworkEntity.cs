using System;
using System.Numerics;
using LiteNetLib;

namespace VoiceCraft.Core.World
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public VoiceCraftNetworkEntity(
            NetPeer netPeer,
            int id,
            Guid userGuid,
            Guid serverUserGuid,
            string locale,
            PositioningType positioningType,
            VoiceCraftWorld world) : base(id, world)
        {
            Name = "New Client";
            NetPeer = netPeer;
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            PositioningType = positioningType;
        }

        public NetPeer NetPeer { get; }
        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public PositioningType PositioningType { get; }

        public override void Reset()
        {
            //Doesn't remove the entity from the world.
            Name = "New Client";
            CaveFactor = 0;
            MuffleFactor = 0;
            WorldId = string.Empty;
            Position = Vector3.Zero;
            Rotation = Vector2.Zero;
            EffectBitmask = ushort.MaxValue;
            TalkBitmask = ushort.MaxValue;
            ListenBitmask = ushort.MaxValue;
        }
    }
}