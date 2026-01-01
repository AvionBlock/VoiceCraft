using System;
using System.Threading.Tasks;
using OpusSharp.Core;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientEntity : VoiceCraftEntity
{
    private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
    private readonly JitterBuffer _jitterBuffer = new(TimeSpan.FromMilliseconds(100));

    private readonly BufferedAudioProvider16 _outputBuffer = new(Constants.OutputBufferShorts)
        { DiscardOnOverflow = true };

    private bool _isVisible;

    private DateTime _lastPacket = DateTime.MinValue;
    private bool _speaking;
    private bool _userMuted;
    private float _volume = 1f;

    public VoiceCraftClientEntity(int id, VoiceCraftWorld world) : base(id, world)
    {
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
            if (!_isVisible)
                Speaking = false;
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

    public void ClearBuffer()
    {
        lock (_outputBuffer)
        lock (_jitterBuffer)
        {
            _outputBuffer.Clear();
            _jitterBuffer.Reset(); //Also reset the jitter buffer.
        }
    }

    public int Read(Span<short> buffer, int count)
    {
        if (_userMuted)
        {
            Speaking = false;
            return 0;
        }

        lock (_outputBuffer)
        {
            if (!Speaking && _outputBuffer.BufferedCount < Constants.PrefillBufferBytes) return 0;
            var read = _outputBuffer.Read(buffer, count);
            if (read <= 0)
            {
                Speaking = false;
                return 0;
            }

            Speaking = true;
            return read;
        }
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

    private int GetNextPacket(Span<short> buffer)
    {
        if (buffer.Length * sizeof(short) < Constants.BytesPerFrame)
            return 0;

        lock (_jitterBuffer)
        {
            try
            {
                if (!_jitterBuffer.Get(out var packet))
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(packet.Data, packet.Data.Length, buffer, Constants.SamplesPerFrame, false);
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
        var readBuffer = new short[Constants.ShortsPerFrame];
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
                _outputBuffer.Write(readBuffer, Constants.BitDepth / 16 * Constants.Channels * read);
            }
            catch (Exception ex)
            {
                LogService.Log(ex);
            }
    }
}