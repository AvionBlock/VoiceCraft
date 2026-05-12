using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Systems;

public class AudioEffectSystem : IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();
    private OrderedDictionary<ushort, IAudioEffect> _defaultAudioEffects = new();

    private volatile ImmutableList<KeyValuePair<ushort, IAudioEffect>> _audioEffectsSnapshot =
        ImmutableList<KeyValuePair<ushort, IAudioEffect>>.Empty;
    
    public ImmutableList<KeyValuePair<ushort, IAudioEffect>> AudioEffects => _audioEffectsSnapshot;

    public OrderedDictionary<ushort, IAudioEffect> DefaultAudioEffects
    {
        get => _defaultAudioEffects;
        set
        {
            _defaultAudioEffects = value;
            Reset();
        }
    }

    ~AudioEffectSystem()
    {
        Dispose(false);
    }

    public event Action<ushort, IAudioEffect?>? OnEffectSet;

    public void Reset()
    {
        ClearEffects();
        foreach (var effect in _defaultAudioEffects) SetEffect(effect.Key, effect.Value);
    }

    public void SetEffect(ushort bitmask, IAudioEffect? effect)
    {
        if (bitmask == ushort.MinValue) return; //Setting a bitmask of 0 does literally nothing.
        lock (_audioEffects)
        {
            switch (effect)
            {
                case null when _audioEffects.Remove(bitmask, out var audioEffect):
                    audioEffect.Dispose();
                    _audioEffectsSnapshot = _audioEffects.ToImmutableList();
                    OnEffectSet?.Invoke(bitmask, null);
                    return;
                case null:
                    return;
            }

            if (_audioEffects.TryGetValue(bitmask, out var oldEffect) && !ReferenceEquals(oldEffect, effect))
                oldEffect.Update(effect);
            else
                _audioEffects[bitmask] = effect;

            _audioEffectsSnapshot = _audioEffects.ToImmutableList();
            OnEffectSet?.Invoke(bitmask, effect);
        }
    }

    public bool TryGetEffect(ushort bitmask, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        lock (_audioEffects)
        {
            return _audioEffects.TryGetValue(bitmask, out effect);
        }
    }

    public void ClearEffects()
    {
        lock (_audioEffects)
        {
            var effects = _audioEffects.ToArray(); //Copy the effects.
            _audioEffects.Clear();
            _audioEffectsSnapshot = ImmutableList<KeyValuePair<ushort, IAudioEffect>>.Empty;
            foreach (var effect in effects)
            {
                effect.Value.Dispose();
                OnEffectSet?.Invoke(effect.Key, null);
            }
        }
    }

    public virtual int Read(Span<float> buffer)
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        ClearEffects();
        OnEffectSet = null;
    }
}