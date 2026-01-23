using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Systems;

public class AudioEffectSystem : IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();
    private OrderedDictionary<ushort, IAudioEffect> _defaultAudioEffects = new();
    private readonly Mutex _mutex = new();

    public IImmutableDictionary<ushort, IAudioEffect> AudioEffects
    {
        get
        {
            lock (_audioEffects)
                return _audioEffects.ToImmutableSortedDictionary();
        }
    }

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
                    OnEffectSet?.Invoke(bitmask, null);
                    return;
                case null:
                    return;
            }

            if (!_audioEffects.TryAdd(bitmask, effect))
                _audioEffects[bitmask] = effect;
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
            foreach (var effect in effects)
            {
                effect.Value.Dispose();
                OnEffectSet?.Invoke(effect.Key, null);
            }
        }
    }

    public int Read(Span<float> buffer, VoiceCraftClient client)
    {
        var bufferLength = buffer.Length;
        var outputBuffer = ArrayPool<float>.Shared.Rent(bufferLength);
        outputBuffer.AsSpan(0, bufferLength).Clear();
        try
        {
            var read = 0;
            Parallel.ForEach(client.VisibleEntities.OfType<VoiceCraftClientEntity>(), x =>
            {
                var entityBuffer = ArrayPool<float>.Shared.Rent(bufferLength);
                var entitySpanBuffer = entityBuffer.AsSpan(0, bufferLength);
                entitySpanBuffer.Clear();
                try
                {
                    var entityRead = ProcessEntityAudio(entitySpanBuffer, x, client);
                    _mutex.WaitOne();
                    read = SampleMixer.Read(entitySpanBuffer[..entityRead], outputBuffer);
                    // ReSharper disable once AccessToModifiedClosure
                    read = Math.Max(read, entityRead);
                    _mutex.ReleaseMutex();
                }
                finally
                {
                    ArrayPool<float>.Shared.Return(entityBuffer);
                }
            });

            outputBuffer.CopyTo(buffer);
            read = SampleHardClip.Read(buffer[..read]);
            read = SampleVolume.Read(buffer[..read], client.OutputVolume);
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(outputBuffer);
        }
    }

    private int ProcessEntityAudio(Span<float> buffer, VoiceCraftClientEntity from, VoiceCraftEntity to)
    {
        var monoCount = buffer.Length / 2;
        var monoBuffer = ArrayPool<float>.Shared.Rent(monoCount);
        var monoSpanBuffer = monoBuffer.AsSpan(0, monoCount);
        monoSpanBuffer.Clear();

        try
        {
            var read = from.Read(monoSpanBuffer);
            if (read <= 0) read = monoCount; //Do a full read.
            read = SampleMonoToStereo.Read(monoSpanBuffer[..read], buffer); //To Stereo
            ProcessEntityEffects(buffer[..read], from, to); //Process Effects
            read = SampleVolume.Read(buffer[..read], from.Volume); //Adjust the volume of the entity.
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(monoBuffer);
        }
    }

    private void ProcessEntityEffects(Span<float> buffer, VoiceCraftClientEntity from, VoiceCraftEntity to)
    {
        lock (_audioEffects)
        {
            foreach (var effect in _audioEffects)
                effect.Value.Process(from, to, effect.Key, buffer);
        }
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
        _mutex.Dispose();
        OnEffectSet = null;
    }
}