using System;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;

namespace VoiceCraft.Client.Audio.Effects;

public class ClientProximityEffect : ProximityEffect
{
    public override void Process(VoiceCraftEntity from, VoiceCraftEntity to, ulong effectBitmask, Span<float> data, int count)
    {
        var bitmask = from.TalkBitmask & to.ListenBitmask & from.EffectBitmask & to.EffectBitmask;
        if ((bitmask & effectBitmask) == 0) return; //Not enabled.

        var range = MaxRange - MinRange;
        if (range == 0) return; //Range is 0. Do not calculate division.
        var distance = Vector3.Distance(from.Position, to.Position);
        var factor = 1f - Math.Clamp((distance - MinRange) / range, 0f, 1f);

        for (var i = 0; i < count; i++) data[i] = Math.Clamp(data[i] * factor, -1f, 1f);
    }
}