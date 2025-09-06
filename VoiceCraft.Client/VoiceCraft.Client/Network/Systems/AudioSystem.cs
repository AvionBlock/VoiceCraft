using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Network.Systems;

public class AudioSystem(VoiceCraftClient client, VoiceCraftWorld world) : IDisposable
{
    private readonly OrderedDictionary<ulong, IAudioEffect> _audioEffects = new();
    private float[] _effectBuffer = [];
    private short[] _entityBuffer = [];

    public IEnumerable<KeyValuePair<ulong, IAudioEffect>> Effects
    {
        get
        {
            lock (_audioEffects)
                return _audioEffects;
        }
    }

    public void Dispose()
    {
        ClearEffects();
        OnEffectSet = null;
        GC.SuppressFinalize(this);
    }

    public event Action<ulong, IAudioEffect?>? OnEffectSet;

    public int Read(Span<short> buffer, int count)
    {
        if (_effectBuffer.Length < count)
            _effectBuffer = new float[count];
        if(_entityBuffer.Length < count)
            _entityBuffer = new short[count];

        var read = 0;
        foreach (var entity in world.Entities.OfType<VoiceCraftClientEntity>().Where(x => x.IsVisible))
        {
            var entityRead = entity.Read(_entityBuffer, count);
            if(entityRead <= 0) continue;
            Pcm16ToFloat(_entityBuffer, entityRead, _effectBuffer); //To IEEEFloat
            ProcessEffects(_effectBuffer, entityRead, entity); //Process Effects
            AdjustVolume(_effectBuffer, entityRead, entity.Volume); //Adjust the volume of the entity.
            PcmFloatTo16(_effectBuffer, entityRead, _entityBuffer); //To PCM16
            Pcm16Mix(_entityBuffer, entityRead, buffer); //Mix 16bit audio.
            read = Math.Max(read, entityRead);
        }
        
        //Full read
        if (read >= count) return read;
        buffer.Slice(read, count - read).Clear();
        return count;
    }

    public bool TryGetEffect(ulong index, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        lock(_audioEffects)
        {
            return _audioEffects.TryGetValue(index, out effect);
        }
    }
    
    public void SetEffect(ulong bitmask, IAudioEffect? effect)
    {
        lock (_audioEffects)
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
    }

    public void ClearEffects()
    {
        lock (_audioEffects)
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

    private void ProcessEffects(Span<float> buffer, int count, VoiceCraftClientEntity entity)
    {
        lock(_audioEffects)
        {
            foreach (var effect in _audioEffects)
                effect.Value.Process(entity, client, effect.Key, buffer, count);
        }
    }

    private static void AdjustVolume(Span<float> buffer, int count, float volume)
    {
        for (var i = 0; i < count; i++)
        {
            buffer[i] *= volume;
        }
    }

    private static void Pcm16ToFloat(Span<short> buffer, int count, Span<float> destBuffer)
    {
        for (var i = 0; i < count; i++)
            destBuffer[i] = buffer[i] / (short.MaxValue + 1f);
    }

    private static void PcmFloatTo16(Span<float> floatBuffer, int count, Span<short> destBuffer)
    {
        for (var i = 0; i < count; i++)
            destBuffer[i] = (short)(floatBuffer[i] * short.MaxValue);
    }
    
    private static void Pcm16Mix(Span<short> srcBuffer, int count, Span<short> dstBuffer)
    {
        for (var i = 0; i < count; i++)
        {
            var mixed = srcBuffer[i] + dstBuffer[i];
            dstBuffer[i] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
        }
    }
}