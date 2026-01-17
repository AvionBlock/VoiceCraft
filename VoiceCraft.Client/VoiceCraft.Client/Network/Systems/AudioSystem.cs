using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;
using VoiceCraft.Network;

namespace VoiceCraft.Client.Network.Systems;

public class AudioSystem(VoiceCraftClient client) : IDisposable
{
    private readonly ConcurrentDictionary<VoiceCraftEntity, AudioSource> _audioSources = new();
    private readonly Mutex _mutex = new();

    public AudioSource GetOrAddAudioSource(VoiceCraftEntity entity)
    {
        return _audioSources.GetOrAdd(entity, (x) => new AudioSource());
    }

    public AudioSource? RemoveAudioSource(VoiceCraftEntity entity)
    {
        _audioSources.TryRemove(entity, out var audioSource);
        return audioSource;
    }

    public int Read(Span<short> buffer, int count)
    {
        //Mono Buffers
        var mixingBuffer = ArrayPool<float>.Shared.Rent(count);
        mixingBuffer.AsSpan().Clear();

        try
        {
            var read = 0;
            Parallel.ForEach(_audioSources, x =>
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

    private int ProcessEntityAudio(KeyValuePair<VoiceCraftEntity, AudioSource> entity, int count, float[] mixingBuffer)
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
            var entityRead = entity.Value.Read(entityBuffer, monoCount);
            if (entityRead <= 0) entityRead = monoCount; //Do a full read.
            entityRead = Sample16ToFloat.Read(entityBuffer, entityRead, monoBuffer); //To IEEEFloat
            entityRead = SampleMonoToStereo.Read(monoBuffer, entityRead, effectBuffer); //To Stereo
            entityRead = ProcessEffects(entity, effectBuffer, entityRead); //Process Effects
            //Adjust the volume of the entity.
            entityRead = SampleVolume.Read(effectBuffer, entityRead, entity.Value.Volume);
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

    private int ProcessEffects(KeyValuePair<VoiceCraftEntity, AudioSource> entity, Span<float> buffer, int count)
    {
        foreach (var effect in client.AudioEffectSystem)
            effect.Value.Process(entity.Key, client, effect.Key, buffer, count);

        return count;
    }

    public void Dispose()
    {
        _mutex.Dispose();
        foreach (var source in _audioSources)
        {
            source.Value.Dispose();
        }
        _audioSources.Clear();
        GC.SuppressFinalize(this);
    }
}