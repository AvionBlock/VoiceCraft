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

    private volatile ImmutableList<KeyValuePair<ushort, IAudioEffect>> _audioEffectsSnapshot =
        ImmutableList<KeyValuePair<ushort, IAudioEffect>>.Empty;

    private readonly Lock _lock = new();

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
        lock (_lock)
        {
            switch (effect)
            {
                case null when _audioEffects.Remove(bitmask, out var audioEffect):
                    _audioEffectsSnapshot = _audioEffects.ToImmutableList();
                    audioEffect.OnDisposed -= RemoveEffect; //Unsubscribe from effect dispose as it has been removed.
                    audioEffect.Dispose();
                    OnEffectSet?.Invoke(bitmask, null);
                    return;
                case null:
                    return;
            }

            effect.Bitmask = bitmask;
            _audioEffects.TryGetValue(bitmask, out var oldEffect);
            if (oldEffect?.EffectType == effect.EffectType)
            {
                oldEffect.Update(effect); //Update old effect with new effect parameters.
                //Don't need to re-update the snapshot as there have been no effect stack changes.
                OnEffectSet?.Invoke(bitmask, oldEffect);
                return;
            }

            effect.OnDisposed += RemoveEffect; //Subscribe to new effect.
            _audioEffects[bitmask] = effect;
            _audioEffectsSnapshot = _audioEffects.ToImmutableList();
            oldEffect?.Dispose(); //Dispose old effect if it exists.
            OnEffectSet?.Invoke(bitmask, effect);
        }

        return;

        void RemoveEffect(IAudioEffect audioEffect)
        {
            lock (_lock)
            {
                audioEffect.OnDisposed -= RemoveEffect;
                if (_audioEffects.Remove(bitmask, out _))
                    //Update Snapshot if successfully removed.
                    _audioEffectsSnapshot = _audioEffects.ToImmutableList();
            }
        }
    }

    public bool TryGetEffect(ushort bitmask, [NotNullWhen(true)] out IAudioEffect? effect)
    {
        lock (_lock)
        {
            return _audioEffects.TryGetValue(bitmask, out effect);
        }
    }

    public void ClearEffects()
    {
        lock (_lock)
        {
            //Clone snapshot and clear everything.
            var effects = _audioEffectsSnapshot.ToArray();
            _audioEffects.Clear();
            _audioEffectsSnapshot = ImmutableList<KeyValuePair<ushort, IAudioEffect>>.Empty;
            foreach (var effect in effects)
            {
                effect.Value.Dispose();
                OnEffectSet?.Invoke(effect.Key, null);
            }
        }
    }

    public int Read(VoiceCraftClient client, Span<float> buffer)
    {
        var bufferLength = buffer.Length;
        var outputBuffer = ArrayPool<float>.Shared.Rent(bufferLength);
        outputBuffer.AsSpan(0, bufferLength).Clear();
        try
        {
            var read = 0;
            Parallel.ForEach(client.World.Entities.OfType<VoiceCraftClientEntity>(), x =>
            {
                var entityBuffer = ArrayPool<float>.Shared.Rent(bufferLength);
                var entitySpanBuffer = entityBuffer.AsSpan(0, bufferLength);
                entitySpanBuffer.Clear();
                try
                {
                    var entityRead = ProcessEntityAudio(x, client, entitySpanBuffer);
                    lock (_lock)
                    {
                        read = SampleMixer.Read(entitySpanBuffer[..entityRead], outputBuffer);
                        // ReSharper disable once AccessToModifiedClosure
                        read = Math.Max(read, entityRead);
                    }
                }
                finally
                {
                    ArrayPool<float>.Shared.Return(entityBuffer);
                }
            });

            outputBuffer[..read].CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(outputBuffer);
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
        OnEffectSet = null;
    }

    private int ProcessEntityAudio(VoiceCraftClientEntity from, VoiceCraftEntity to, Span<float> buffer)
    {
        var read = from.Read(buffer);
        if (read <= 0) read = buffer.Length; //Do a full read.
        ProcessEntityEffects(from, to, buffer[..read]); //Process Effects
        read = SampleVolume.Read(buffer[..read], from.Volume); //Adjust the volume of the entity.
        return read;
    }

    private void ProcessEntityEffects(VoiceCraftClientEntity from, VoiceCraftEntity to, Span<float> buffer)
    {
        var snapshot = _audioEffectsSnapshot;
        foreach (var effect in snapshot)
        {
            if (!from.TryGetEffectProcessor(effect.Key, out var processor))
            {
                processor = effect.Value.GetProcessor(from);
                from.SetEffectProcessor(effect.Key, processor);
            }

            processor.Process(to, buffer);
        }
    }
}
