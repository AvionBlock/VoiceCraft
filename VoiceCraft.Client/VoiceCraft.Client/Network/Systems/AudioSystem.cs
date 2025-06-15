using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Network.Systems;

public class AudioSystem(VoiceCraftClient client, VoiceCraftWorld world) : IDisposable
{
    private readonly Dictionary<byte, IAudioEffect> _audioEffects = new();
    private float[] _effectBuffer = [];
    private short[] _entityBuffer = [];

    public IEnumerable<KeyValuePair<byte, IAudioEffect>> Effects
    {
        get
        {
            lock (_audioEffects)
            {
                var audioEffects = _audioEffects.ToArray();
                return audioEffects;
            }
        }
    }

    public void Dispose()
    {
        ClearEffects();
        OnEffectSet = null;
        OnEffectRemoved = null;
        GC.SuppressFinalize(this);
    }

    public event Action<byte, IAudioEffect>? OnEffectSet;
    public event Action<byte, IAudioEffect>? OnEffectRemoved;

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
        
        return read;
    }

    public void AddEffect(IAudioEffect effect)
    {
        lock(_audioEffects)
        {
            var id = GetLowestAvailableId();
            if (_audioEffects.TryAdd(id, effect))
                throw new InvalidOperationException("Failed to add effect!");
        }
    }

    public void SetEffect(byte index, IAudioEffect effect)
    {
        lock(_audioEffects)
        {
            if (!_audioEffects.TryAdd(index, effect))
                _audioEffects[index] = effect;
            OnEffectSet?.Invoke(index, effect);
        }
    }

    public bool TryGetEffect(byte index, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        lock(_audioEffects)
        {
            effect = _audioEffects.GetValueOrDefault(index);
            return effect != null;
        }
    }

    public void RemoveEffect(byte index)
    {
        lock(_audioEffects)
        {
            if (!_audioEffects.Remove(index, out var effect))
                throw new InvalidOperationException("Failed to remove effect!");
            effect.Dispose();
            OnEffectRemoved?.Invoke(index, effect);
        }
    }

    public void ClearEffects()
    {
        lock(_audioEffects)
        {
            var effects = _audioEffects.ToArray(); //Copy the effects.
            _audioEffects.Clear();
            foreach (var effect in effects)
            {
                effect.Value.Dispose();
                OnEffectRemoved?.Invoke(effect.Key, effect.Value);
            }
        }
    }

    private byte GetLowestAvailableId()
    {
        for (var i = byte.MinValue; i < byte.MaxValue; i++)
            if (!_audioEffects.ContainsKey(i))
                return i;

        throw new InvalidOperationException("Could not find an available id!");
    }

    private void ProcessEffects(Span<float> buffer, int count, VoiceCraftClientEntity entity)
    {
        lock(_audioEffects)
        {
            foreach (var effect in _audioEffects)
                effect.Value.Process(entity, client, buffer, count);
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