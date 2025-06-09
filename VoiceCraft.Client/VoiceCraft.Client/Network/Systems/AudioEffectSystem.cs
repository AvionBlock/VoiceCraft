using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Network.Systems;

public class AudioEffectSystem(VoiceCraftClient client) : IDisposable
{
    private readonly Dictionary<byte, IAudioEffect> _audioEffects = new();

    private readonly Lock _lockObj = new();
    private float[] _floatBuffer = [];

    public IEnumerable<KeyValuePair<byte, IAudioEffect>> Effects
    {
        get
        {
            _lockObj.Enter();
            var audioEffects = _audioEffects.ToArray();
            _lockObj.Exit();
            return audioEffects;
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

    public void ProcessEffects(Span<short> buffer, int count, VoiceCraftClientEntity entity)
    {
        if (_floatBuffer.Length < count)
            _floatBuffer = new float[count];
        Pcm16ToFloat(buffer, count, _floatBuffer); //To float

        _lockObj.Enter();
        try
        {
            foreach (var effect in _audioEffects) effect.Value.Process(entity, client, _floatBuffer, count);
            for (var i = 0; i < count; i++)
            {
                _floatBuffer[i] *= entity.Volume;
            }
        }
        finally
        {
            _lockObj.Exit();
        }

        PcmFloatTo16(_floatBuffer, count, buffer); //To 16bit
    }

    public void AddEffect(IAudioEffect effect)
    {
        _lockObj.Enter();
        try
        {
            var id = GetLowestAvailableId();
            if (_audioEffects.TryAdd(id, effect))
                throw new InvalidOperationException("Failed to add effect!");
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    public void SetEffect(byte index, IAudioEffect effect)
    {
        _lockObj.Enter();
        try
        {
            if (!_audioEffects.TryAdd(index, effect))
                _audioEffects[index] = effect;
            OnEffectSet?.Invoke(index, effect);
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    public bool TryGetEffect(byte index, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        _lockObj.Enter();
        try
        {
            effect = _audioEffects.GetValueOrDefault(index);
            return effect != null;
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    public void RemoveEffect(byte index)
    {
        _lockObj.Enter();
        try
        {
            if (!_audioEffects.Remove(index, out var effect))
                throw new InvalidOperationException("Failed to remove effect!");
            effect.Dispose();
            OnEffectRemoved?.Invoke(index, effect);
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    public void ClearEffects()
    {
        _lockObj.Enter();
        try
        {
            var effects = _audioEffects.ToArray(); //Copy the effects.
            _audioEffects.Clear();
            foreach (var effect in effects)
            {
                effect.Value.Dispose();
                OnEffectRemoved?.Invoke(effect.Key, effect.Value);
            }
        }
        finally
        {
            _lockObj.Exit();
        }
    }

    private byte GetLowestAvailableId()
    {
        for (var i = byte.MinValue; i < byte.MaxValue; i++)
            if (!_audioEffects.ContainsKey(i))
                return i;

        throw new InvalidOperationException("Could not find an available id!");
    }

    private static void Pcm16ToFloat(Span<short> buffer, int count, Span<float> destBuffer)
    {
        for (var i = 0; i < count; i++) destBuffer[i] = buffer[i] / (short.MaxValue + 1f);
    }

    private static void PcmFloatTo16(Span<float> floatBuffer, int count, Span<short> buffer)
    {
        for (var i = 0; i < count; i++) buffer[i] = (short)(floatBuffer[i] * short.MaxValue);
    }
}