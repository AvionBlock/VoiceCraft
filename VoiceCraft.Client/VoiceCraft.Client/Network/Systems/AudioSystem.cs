using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network.Systems;

public class AudioSystem(VoiceCraftClient client, VoiceCraftWorld world) : IDisposable
{
    private readonly OrderedDictionary<ushort, IAudioEffect> _audioEffects = new();
    private readonly Mutex _mutex = new();

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
        var mixingBuffer = ArrayPool<float>.Shared.Rent(count);
        mixingBuffer.AsSpan().Clear();

        try
        {
            var read = 0;
            Parallel.ForEach(world.Entities.OfType<VoiceCraftClientEntity>(), x =>
            {
                var entityRead = ProcessEntityAudio(x, count, mixingBuffer);
                _mutex.WaitOne();
                // ReSharper disable once AccessToModifiedClosure
                read = Math.Max(read, entityRead);
                _mutex.ReleaseMutex();
            });
            read = SampleHardClip.Read(mixingBuffer, read);
            read = SampleVolume.Read(mixingBuffer, read, client.OutputVolume);
            read = SampleFloatTo16.Read(mixingBuffer, read, buffer); //To PCM16
            //Full read
            if (read >= count) return read;
            buffer.Slice(read, count - read).Clear();
            return count;
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(mixingBuffer);
        }

        return 0;
    }

    private int ProcessEntityAudio(VoiceCraftClientEntity entity, int count, float[] mixingBuffer)
    {
        var monoCount = count / 2;
        var entityBuffer = ArrayPool<short>.Shared.Rent(monoCount);
        var monoBuffer = ArrayPool<float>.Shared.Rent(monoCount);
        var effectBuffer = ArrayPool<float>.Shared.Rent(count);

        entityBuffer.AsSpan().Clear();
        monoBuffer.AsSpan().Clear();
        effectBuffer.AsSpan().Clear();
        try
        {
            var entityRead = entity.Read(entityBuffer, monoCount);
            if (entityRead <= 0) entityRead = monoCount; //Do a full read.
            entityRead = Sample16ToFloat.Read(entityBuffer, entityRead, monoBuffer); //To IEEEFloat
            entityRead = SampleMonoToStereo.Read(monoBuffer, entityRead, effectBuffer); //To Stereo
            entityRead = ProcessEffects(effectBuffer, entityRead, entity); //Process Effects
            entityRead = SampleVolume.Read(effectBuffer, entityRead, entity.Volume); //Adjust the volume of the entity.
            lock (mixingBuffer)
            {
                entityRead = SampleMixer.Read(effectBuffer, entityRead, mixingBuffer); //Mix IEEFloat audio.
            }

            return entityRead;
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
        finally
        {
            ArrayPool<short>.Shared.Return(entityBuffer);
            ArrayPool<float>.Shared.Return(monoBuffer);
            ArrayPool<float>.Shared.Return(effectBuffer);
        }

        return 0;
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
}