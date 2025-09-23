using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Server.Systems;

public class AudioEffectSystem : IResettable, IDisposable
{
    private readonly OrderedDictionary<uint, IAudioEffect> _audioEffects = new()
    {
        { uint.MaxValue, new EchoEffect(Constants.SampleRate, 0.2f) }
    };

    public IEnumerable<KeyValuePair<uint, IAudioEffect>> Effects => _audioEffects;

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

    public event Action<uint, IAudioEffect?>? OnEffectSet;

    public void SetEffect(uint bitmask, IAudioEffect? effect)
    {
        if (effect == null && _audioEffects.Remove(bitmask, out var audioEffect))
        {
            audioEffect.Dispose();
            OnEffectSet?.Invoke(bitmask, null);
            return;
        }

        if (effect == null || !_audioEffects.TryAdd(bitmask, effect)) return;
        OnEffectSet?.Invoke(bitmask, effect);
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