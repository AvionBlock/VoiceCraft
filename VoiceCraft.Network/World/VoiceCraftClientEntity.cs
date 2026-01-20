using System;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Audio;

namespace VoiceCraft.Network.World;

public class VoiceCraftClientEntity : VoiceCraftEntity
{
    private readonly IAudioDecoder _decoder;
    private readonly JitterBuffer _jitterBuffer = new(TimeSpan.FromMilliseconds(100));

    private readonly SampleBufferProvider<float> _outputBuffer = new(Constants.OutputBufferSamples)
        { PrefillSize = Constants.PrefillBufferSamples };

    private DateTime _lastPacket = DateTime.MinValue;
    private bool _speaking;
    private bool _userMuted;
    private bool _isVisible;
    private float _volume = 1f;

    public VoiceCraftClientEntity(int id, IAudioDecoder decoder) : base(id)
    {
        _decoder = decoder;
        Task.Run(TaskLogicAsync);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnIsVisibleUpdated?.Invoke(_isVisible, this);
            if (_isVisible) return;
            Speaking = false;
            ClearBuffer();
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            if (Math.Abs(_volume - value) < Constants.FloatingPointTolerance) return;
            _volume = value;
            OnVolumeUpdated?.Invoke(_volume, this);
        }
    }

    public bool UserMuted
    {
        get => _userMuted;
        set
        {
            if (_userMuted == value) return;
            _userMuted = value;
            OnUserMutedUpdated?.Invoke(_userMuted, this);
        }
    }

    public bool Speaking
    {
        get => _speaking;
        set
        {
            if (_speaking == value) return;
            _speaking = value;
            if (value) OnStartedSpeaking?.Invoke(this);
            else OnStoppedSpeaking?.Invoke(this);
        }
    }

    public event Action<bool, VoiceCraftClientEntity>? OnIsVisibleUpdated;
    public event Action<float, VoiceCraftClientEntity>? OnVolumeUpdated;
    public event Action<bool, VoiceCraftClientEntity>? OnUserMutedUpdated;
    public event Action<VoiceCraftClientEntity>? OnStartedSpeaking;
    public event Action<VoiceCraftClientEntity>? OnStoppedSpeaking;

    public int Read(Span<float> buffer)
    {
        if (_userMuted)
        {
            Speaking = false;
            return 0;
        }

        var read = _outputBuffer.Read(buffer);
        if (read <= 0)
        {
            Speaking = false;
            return 0;
        }

        Speaking = true;
        return read;
    }

    public override void ReceiveAudio(byte[] buffer, ushort timestamp, float frameLoudness)
    {
        lock (_jitterBuffer)
        {
            var packet = new JitterPacket(timestamp, buffer);
            _jitterBuffer.Add(packet);
        }

        base.ReceiveAudio(buffer, timestamp, frameLoudness);
    }

    public override void Destroy()
    {
        lock (_decoder)
        lock (_jitterBuffer)
        {
            _jitterBuffer.Reset();
            _decoder.Dispose();
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
        lock (_jitterBuffer)
        {
            _outputBuffer.Reset();
            _jitterBuffer.Reset(); //Also reset the jitter buffer.
        }
    }

    private int GetNextPacket(Span<float> buffer)
    {
        if (buffer.Length < Constants.SamplesPerFrame)
            return 0;

        lock (_jitterBuffer)
        {
            try
            {
                if (!_jitterBuffer.Get(out var packet))
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, buffer, Constants.SamplesPerFrame);

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(packet.Data, buffer, Constants.SamplesPerFrame);
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
        var readBuffer = new float[Constants.SamplesPerFrame];
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
                if (read <= 0 || _userMuted) continue;
                _outputBuffer.Write(readBuffer.AsSpan(0, read));
            }
            catch
            {
                //Do Nothing
            }
    }
}