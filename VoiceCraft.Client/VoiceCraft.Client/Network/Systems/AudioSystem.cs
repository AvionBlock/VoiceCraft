using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network.Systems;

public class AudioSystem(VoiceCraftClient client, VoiceCraftWorld world) : IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();
    private float[] _effectBuffer = [];
    private short[] _entityBuffer = [];
    private float[] _mixingBuffer = [];
    private float[] _monoBuffer = [];

    public IEnumerable<KeyValuePair<ushort, IAudioEffect>> Effects
    {
        get
        {
            lock (_audioEffects)
            {
                return _audioEffects;
            }
        }
    }

    public void Dispose()
    {
        ClearEffects();
        OnEffectSet = null;
        GC.SuppressFinalize(this);
    }

    public event Action<ushort, IAudioEffect?>? OnEffectSet;

    public int Read(Span<short> buffer, int count)
    {
        //Mono Buffers
        var monoCount = count / 2;
        if (_entityBuffer.Length < monoCount)
            _entityBuffer = new short[monoCount];
        if (_monoBuffer.Length < monoCount)
            _monoBuffer = new float[monoCount];

        //Stereo Buffers
        if (_effectBuffer.Length < count)
            _effectBuffer = new float[count];
        if (_mixingBuffer.Length < count)
            _mixingBuffer = new float[count];

        _entityBuffer.AsSpan().Clear();
        _monoBuffer.AsSpan().Clear();
        _mixingBuffer.AsSpan().Clear();
        _effectBuffer.AsSpan().Clear();

        var read = 0;
        foreach (var entity in world.Entities.OfType<VoiceCraftClientEntity>().Where(x => x.IsVisible))
            try
            {
                var entityRead = entity.Read(_entityBuffer, monoCount);
                if (entityRead <= 0) entityRead = monoCount; //Do a full read.
                entityRead = Pcm16ToFloat(_entityBuffer, entityRead, _monoBuffer); //To IEEEFloat
                entityRead = PcmFloatMonoToStereo(_monoBuffer, entityRead, _effectBuffer); //To Stereo
                entityRead = ProcessEffects(_effectBuffer, entityRead, entity); //Process Effects
                entityRead = AdjustVolume(_effectBuffer, entityRead, entity.Volume); //Adjust the volume of the entity.
                entityRead = PcmFloatMix(_effectBuffer, entityRead, _mixingBuffer); //Mix IEEFloat audio.
                entityRead = PcmFloatTo16(_mixingBuffer, entityRead, buffer); //To PCM16
                read = Math.Max(read, entityRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        //Full read
        if (read >= count) return read;
        buffer.Slice(read, count - read).Clear();
        return count;
    }

    public bool TryGetEffect(ushort bitmask, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        lock (_audioEffects)
        {
            return _audioEffects.TryGetValue(bitmask, out effect);
        }
    }

    public void SetEffect(ushort bitmask, IAudioEffect? effect)
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

    private int ProcessEffects(Span<float> buffer, int count, VoiceCraftClientEntity entity)
    {
        lock (_audioEffects)
        {
            foreach (var effect in _audioEffects)
                effect.Value.Process(entity, client, effect.Key, buffer, count);
        }

        return count;
    }

    private static int AdjustVolume(Span<float> buffer, int count, float volume)
    {
        for (var i = 0; i < count; i++) buffer[i] *= volume;
        return count;
    }

    private static int Pcm16ToFloat(Span<short> buffer, int count, Span<float> destBuffer)
    {
        for (var i = 0; i < count; i++)
            destBuffer[i] = Math.Clamp(buffer[i] / (short.MaxValue + 1f), -1f, 1f);
        return count;
    }

    private static int PcmFloatMonoToStereo(Span<float> buffer, int count, Span<float> destBuffer)
    {
        var destOffset = 0;
        for (var i = 0; i < count; i++)
        {
            var sampleVal = buffer[i];
            destBuffer[destOffset++] = sampleVal;
            destBuffer[destOffset++] = sampleVal;
        }

        return count * 2;
    }

    private static int PcmFloatTo16(Span<float> floatBuffer, int count, Span<short> destBuffer)
    {
        for (var i = 0; i < count; i++)
            destBuffer[i] = Math.Clamp((short)(floatBuffer[i] * short.MaxValue), short.MinValue, short.MaxValue);

        return count;
    }

    private static int PcmFloatMix(Span<float> srcBuffer, int count, Span<float> dstBuffer)
    {
        for (var i = 0; i < count; i++) dstBuffer[i] = Math.Clamp(srcBuffer[i] + dstBuffer[i], -1f, 1f);

        return count;
    }
}