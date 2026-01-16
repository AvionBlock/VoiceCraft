using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.Systems;

public class AudioEffectSystem : IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();
    private OrderedDictionary<ushort, IAudioEffect> _defaultAudioEffects = new();

    public OrderedDictionary<ushort, IAudioEffect> DefaultAudioEffects
    {
        get => _defaultAudioEffects;
        set
        {
            _defaultAudioEffects = value;
            Reset();
        }
    }

    public IEnumerable<KeyValuePair<ushort, IAudioEffect>> Effects => _audioEffects;

    public void Dispose()
    {
        ClearEffects();
        OnEffectSet = null;
        GC.SuppressFinalize(this);
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
        switch (effect)
        {
            case null when _audioEffects.Remove(bitmask, out var audioEffect):
                audioEffect.Dispose();
                OnEffectSet?.Invoke(bitmask, null);
                return;
            case null:
                return;
        }

        if (!_audioEffects.TryAdd(bitmask, effect))
            _audioEffects[bitmask] = effect;
        OnEffectSet?.Invoke(bitmask, effect);
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
        var effects = _audioEffects.ToArray(); //Copy the effects.
        _audioEffects.Clear();
        foreach (var effect in effects)
        {
            effect.Value.Dispose();
            OnEffectSet?.Invoke(effect.Key, null);
        }
    }
}