using System;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;

namespace VoiceCraft.Client.Audio.Effects
{
    public class ClientProximityEffect : ProximityEffect
    {
        public override void Process(VoiceCraftEntity from, VoiceCraftEntity to, Span<float> data, int count)
        {
            var bitmask = from.TalkBitmask & to.ListenBitmask;
            if ((bitmask & Bitmask) == 0) return; //Not enabled.
            
            var minRange = from.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMinRange);
            var toMinRange = to.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMinRange);
            if (minRange == null && toMinRange == null)
                minRange = MaxRange;
            else
                minRange = Math.Min(minRange ?? int.MaxValue, minRange ?? int.MaxValue);
            
            var maxRange = from.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMaxRange);
            var toMaxRange = to.GetPropertyOrDefault<int>(PropertyKey.ProximityEffectMaxRange);
            if (maxRange == null && toMaxRange == null)
                maxRange = MaxRange;
            else
                maxRange = Math.Max(maxRange ?? int.MinValue, toMaxRange ?? int.MinValue);
            
            var range = (int)(maxRange - minRange);
            if(range == 0) return; //Range is 0. Do not calculate division.
            var distance = Vector3.Distance(from.Position, to.Position);
            var factor = 1f - Math.Clamp(distance / range, 0f, 1.0f);
            
            for (var i = 0; i < count; i++)
            {
                data[i] = Math.Clamp(data[i] * factor, -1f, 1f);
            }
        }
    }
}