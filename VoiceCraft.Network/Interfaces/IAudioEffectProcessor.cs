using System;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.Interfaces;

public interface IAudioEffectProcessor : IDisposable
{
    IAudioEffect Effect { get; }
    VoiceCraftEntity Entity { get; }
    event Action<IAudioEffectProcessor>? OnDisposed;
    void Process(VoiceCraftEntity to, Span<float> buffer);
}