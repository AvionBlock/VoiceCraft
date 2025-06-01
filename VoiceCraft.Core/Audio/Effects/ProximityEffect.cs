using System;
using System.Numerics;
using LiteNetLib.Utils;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Audio.Effects
{
    public class ProximityEffect : IAudioEffect, IVisible
    {
        public int MinRange { get; set; }
        public int MaxRange { get; set; }
        public EffectType EffectType => EffectType.Proximity;

        public ulong Bitmask { get; set; } = ulong.MaxValue;

        public virtual void Process(VoiceCraftEntity from, VoiceCraftEntity to, Span<float> data, int count)
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
            Bitmask = reader.GetULong();
            MinRange = reader.GetInt();
            MaxRange = reader.GetInt();
        }

        public void Dispose()
        {
            //Nothing to dispose.
        }


        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask;
            if ((bitmask & Bitmask) == 0) return true; //Proximity checking disabled.

            var maxRange = from.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMaxRange);
            var toMaxRange = to.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMaxRange);
            if (maxRange == null && toMaxRange == null)
                maxRange = MaxRange;
            else
                maxRange = Math.Max(maxRange ?? int.MinValue, toMaxRange ?? int.MinValue);

            var distance = Vector3.Distance(from.Position, to.Position);
            return distance <= maxRange;
        }
    }
}