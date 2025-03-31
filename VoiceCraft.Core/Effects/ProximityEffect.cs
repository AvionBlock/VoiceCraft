using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Effects
{
    public struct ProximityEffect : IAudioEffect, IVisible
    {
        public EffectType EffectType => EffectType.Proximity;
        public ulong Bitmask { get; private set; }
        public uint MinRange { get; private set; }
        public uint MaxRange { get; private set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Bitmask);
            writer.Put(MinRange);
            writer.Put(MaxRange);
        }

        public void Deserialize(NetDataReader reader)
        {
            //Do this first before actually assigning the data.
            var bitmask = reader.GetULong();
            var minRange = reader.GetUInt();
            var maxRange = reader.GetUInt();
            
            Bitmask = bitmask;
            MinRange = minRange;
            MaxRange = maxRange;
        }

        public bool VisibleTo(VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity, ulong bitmask)
        {
            if ((bitmask & Bitmask) == 0) return true; //Disabled, Is visible.
            var distance = Vector3.Distance(fromEntity.Position, toEntity.Position);
            var maxRange = MaxRange; //Need to do a check for entity states.
            
            return distance <= maxRange;
        }
    }
}