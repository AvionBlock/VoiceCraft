using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect : INetSerializable, IDisposable
    {
        EffectType EffectType { get; }

        void Process(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask, Span<float> data, int count);
    }
}