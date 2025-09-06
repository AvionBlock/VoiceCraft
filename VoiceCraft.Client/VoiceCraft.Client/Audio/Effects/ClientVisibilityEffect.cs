using System;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;

namespace VoiceCraft.Client.Audio.Effects;

public class ClientVisibilityEffect : VisibilityEffect
{
    public override void Process(VoiceCraftEntity from, VoiceCraftEntity to, ulong effectBitmask, Span<float> data, int count)
    {
    }
}