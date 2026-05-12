using System;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Interfaces;

public interface IAudioEffectProcessor<T> : IDisposable where T : IAudioEffect
{
    T AudioEffect { get; }
    void Process(VoiceCraftEntity to, Span<float> buffer);
}