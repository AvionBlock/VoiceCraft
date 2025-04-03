using System;
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
        public int MinRange { get; private set; }
        public int MaxRange { get; private set; }

        public bool ApplyEffect(byte[] data, uint sampleRate, uint channels, VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity)
        {
            throw new NotSupportedException();
        }

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
            var minRange = reader.GetInt();
            var maxRange = reader.GetInt();
            
            Bitmask = bitmask;
            MinRange = minRange;
            MaxRange = maxRange;
        }

        public bool VisibleTo(VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity, ulong bitmask)
        {
            if ((bitmask & Bitmask) == 0) return true; //Disabled, Is visible.
            var distance = Vector3.Distance(fromEntity.Position, toEntity.Position);
            var maxRange = MaxRange;
            //Checks for entity states that override the default value.
            maxRange = Math.Max(maxRange, fromEntity.GetIntProperty($"{GetType().Name}:{nameof(MaxRange)}") ?? 0);
            maxRange = Math.Max(maxRange, toEntity.GetIntProperty($"{GetType().Name}:{nameof(MaxRange)}") ?? 0);
            
            return distance <= maxRange;
        }
    }
}