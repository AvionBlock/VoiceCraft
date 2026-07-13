using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Audio;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network.World;

public class VoiceCraftClientEntity : VoiceCraftEntity
{
    private readonly IAudioDecoder _decoder;
    private readonly JitterBuffer _jitterBuffer = new(TimeSpan.FromMilliseconds(100));

    private readonly SampleBufferProvider<float> _outputBuffer =
        new(Constants.OutputBufferSize)
            { PrefillSize = Constants.PrefillBufferSize };

    private DateTime _lastPacket = DateTime.MinValue;
    private readonly ConcurrentDictionary<ushort, IAudioEffectProcessor> _effectProcessors = new();

    private readonly Lock _lock = new(); // For packet decoding.

    public VoiceCraftClientEntity(int id, IAudioDecoder decoder) : base(id)
    {
        _decoder = decoder;
        Task.Run(TaskLogicAsync);
    }


    public bool IsVisible
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnIsVisibleUpdated?.Invoke(field, this);
            if (field) return;
            Speaking = false;
            ClearBuffer();
        }
    }

    public float Volume
    {
        get;
        set
        {
            if (Math.Abs(field - value) < Constants.FloatingPointTolerance) return;
            field = value;
            OnVolumeUpdated?.Invoke(field, this);
        }
    } = 1f;

    public bool UserMuted
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnUserMutedUpdated?.Invoke(field, this);
        }
    }

    public bool Speaking
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            if (value) OnStartedSpeaking?.Invoke(this);
            else OnStoppedSpeaking?.Invoke(this);
        }
    }

    public event Action<bool, VoiceCraftClientEntity>? OnIsVisibleUpdated;
    public event Action<float, VoiceCraftClientEntity>? OnVolumeUpdated;
    public event Action<bool, VoiceCraftClientEntity>? OnUserMutedUpdated;
    public event Action<VoiceCraftClientEntity>? OnStartedSpeaking;
    public event Action<VoiceCraftClientEntity>? OnStoppedSpeaking;

    public void SetEffectProcessor(ushort bitmask, IAudioEffectProcessor? processor)
    {
        switch (processor)
        {
            case null when _effectProcessors.Remove(bitmask, out var effectProcessor):
                effectProcessor.OnDisposed -= RemoveEffect; //Unsubscribe from effect dispose as it has been removed.
                effectProcessor.Dispose();
                return;
            case null:
                return;
        }

        if (_effectProcessors.TryGetValue(bitmask, out var oldProcessor))
        {
            //Dispose old processor if it exists. We dispose properly after switching out processor.
            oldProcessor.OnDisposed -= RemoveEffect;
        }

        //Set new processor.
        processor.OnDisposed += RemoveEffect; //Subscribe to new effect.
        _effectProcessors[bitmask] = processor;
        oldProcessor?.Dispose();
    }

    public bool TryGetEffectProcessor(ushort bitmask, [NotNullWhen(true)] out IAudioEffectProcessor? effect)
    {
        return _effectProcessors.TryGetValue(bitmask, out effect);
    }

    public int Read(Span<float> buffer)
    {
        var read = 0;
        if (UserMuted)
        {
            Speaking = false;
            buffer.Clear();
            return read;
        }

        var monoSize = buffer.Length / 2;
        var monoBuffer = ArrayPool<float>.Shared.Rent(monoSize);
        var monoSpanBuffer = monoBuffer.AsSpan(0, monoSize);
        try
        {
            read = _outputBuffer.Read(monoSpanBuffer);

            if (read <= 0)
            {
                Speaking = false;
                return 0;
            }

            read = SampleMonoToStereo.Read(monoSpanBuffer[..read], buffer);
            Speaking = true;
            return read;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(monoBuffer);
        }
    }

    public override void ReceiveAudio(byte[] buffer, ushort timestamp, float frameLoudness)
    {
        var packet = new JitterPacket(timestamp, buffer);
        _jitterBuffer.Add(packet);
        base.ReceiveAudio(buffer, timestamp, frameLoudness);
    }

    public override void Destroy()
    {
        if (Destroyed) return;

        _jitterBuffer.Reset();
        lock (_lock)
        {
            _decoder.Dispose();
        }

        var effects = _effectProcessors.ToArray();
        _effectProcessors.Clear();
        foreach (var effect in effects)
        {
            effect.Value.Dispose();
        }

        base.Destroy();

        OnIsVisibleUpdated = null;
        OnVolumeUpdated = null;
        OnUserMutedUpdated = null;
        OnStartedSpeaking = null;
        OnStoppedSpeaking = null;
    }

    private void ClearBuffer()
    {
        _outputBuffer.Reset();
        lock (_lock)
        {
            _jitterBuffer.Reset(); //Also reset the jitter buffer.
            _lastPacket = DateTime.MinValue;
        }
    }
    
    private void RemoveEffect(IAudioEffectProcessor effectProcessor)
    {
        effectProcessor.OnDisposed -= RemoveEffect;
        _effectProcessors.Remove(effectProcessor.Effect.Bitmask, out _);
    }

    private int GetNextPacket(Span<float> buffer)
    {
        if (buffer.Length < Constants.FrameSize)
            return 0;

        lock (_lock)
        {
            try
            {
                if (!_jitterBuffer.Get(out var packet))
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, buffer, Constants.FrameSize);

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(packet.Data, buffer, Constants.FrameSize);
            }
            catch
            {
                return 0;
            }
        }
    }

    private async Task TaskLogicAsync()
    {
        var startTick = Environment.TickCount;
        var readBuffer = new float[Constants.FrameSize];
        while (!Destroyed)
            try
            {
                var dist = (long)(startTick - Environment.TickCount); //Wraparound
                if (dist > 0)
                {
                    await Task.Delay((int)dist).ConfigureAwait(false);
                    continue;
                }

                startTick += Constants.FrameSizeMs; //Step Forwards.
                Array.Clear(readBuffer); //Clear Read Buffer.

                var read = GetNextPacket(readBuffer);
                if (read <= 0 || UserMuted) continue;
                _outputBuffer.Write(readBuffer.AsSpan(0, read));
            }
            catch
            {
                //Do Nothing
            }
    }
}
