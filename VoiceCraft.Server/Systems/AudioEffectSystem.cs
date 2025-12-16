using System.Diagnostics.CodeAnalysis;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems;

public class AudioEffectSystem : IResettable, IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();

    public IEnumerable<KeyValuePair<ushort, IAudioEffect>> Effects => _audioEffects;

    public void Dispose()
    {
        ClearEffects();
        OnEffectSet = null;
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        ClearEffects();
    }

    public event Action<ushort, IAudioEffect?>? OnEffectSet;

    public void SetEffect(ushort bitmask, IAudioEffect? effect)
    {
        switch (effect)
        {
            case null when _audioEffects.Remove(bitmask, out var audioEffect):
                audioEffect.Dispose();
                OnEffectSet?.Invoke(bitmask, null);
                return;
            case null:
                return;
        }

        if(!_audioEffects.TryAdd(bitmask, effect))
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