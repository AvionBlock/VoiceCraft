using System;
using NAudio.Wave;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientEntity(int id, VoiceCraftWorld world) : VoiceCraftEntity(id, world)
{
    private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
    private readonly byte[] _encodedData = new byte[Constants.MaximumEncodedBytes];
    private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);

    private readonly BufferedWaveProvider _outputBuffer = new(new WaveFormat(Constants.SamplesPerFrame, Constants.Channels))
    {
        ReadFully = false,
        DiscardOnBufferOverflow = true
    };

    private long _startTick = Environment.TickCount64;
    private readonly byte[] _readBuffer = new byte[Constants.BytesPerFrame];
    private bool _isReady;
    private bool _isVisible;
    private DateTime _lastPacket = DateTime.MinValue;
    private float _volume = 1f;
    private bool _userMuted;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnIsVisibleUpdated?.Invoke(_isVisible, this);
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
            if(_userMuted == value) return;
            _userMuted = value;
            OnUserMutedUpdated?.Invoke(_userMuted, this);
        }
    }

    public event Action<bool, VoiceCraftClientEntity>? OnIsVisibleUpdated;
    public event Action<float, VoiceCraftClientEntity>? OnVolumeUpdated;
    public event Action<bool, VoiceCraftClientEntity>? OnUserMutedUpdated;

    public void Tick()
    {
        if (Destroyed) return;
        
        var currentTick = Environment.TickCount;
        while (_startTick - currentTick <= 0)
        {
            _startTick += Constants.FrameSizeMs;
            Array.Clear(_readBuffer);
            var read = Read(_readBuffer);
            if (read > 0)
                _outputBuffer.AddSamples(_readBuffer, 0, _readBuffer.Length);

            if (_outputBuffer.BufferedDuration >= TimeSpan.FromMilliseconds(Constants.DecodeBufferSizeThresholdMs))
                _isReady = true;
        }
    }

    public void ClearBuffer()
    {
        _outputBuffer.ClearBuffer();
        lock (_jitterBuffer)
        {
            _jitterBuffer.Reset(); //Also reset the jitter buffer.
        }
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (!_isReady) return 0;
        var read = _outputBuffer.Read(buffer, offset, count);
        if (read == 0)
            _isReady = false;
        return read;
    }

    public override void ReceiveAudio(byte[] buffer, uint timestamp, float frameLoudness)
    {
        lock (_jitterBuffer)
        {
            var inPacket = new SpeexDSPJitterBufferPacket(buffer, (uint)buffer.Length)
            {
                timestamp = timestamp,
                span = Constants.SamplesPerFrame
            };
            _jitterBuffer.Put(ref inPacket);
        }

        base.ReceiveAudio(buffer, timestamp, frameLoudness);
    }

    public override void Destroy()
    {
        lock (_jitterBuffer)
        {
            _jitterBuffer.Dispose();
        }

        _decoder.Dispose();
        base.Destroy();

        OnIsVisibleUpdated = null;
        OnVolumeUpdated = null;
        OnUserMutedUpdated = null;
    }

    private int Read(byte[] buffer)
    {
        if (buffer.Length < Constants.BytesPerFrame)
            return 0;

        try
        {
            Array.Clear(_encodedData);
            var outPacket = new SpeexDSPJitterBufferPacket(_encodedData, (uint)_encodedData.Length);
            var startOffset = 0;
            lock (_jitterBuffer)
            {
                if (_jitterBuffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
            }

            _lastPacket = DateTime.UtcNow;
            return _decoder.Decode(_encodedData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
        }
        catch
        {
            return 0;
        }
        finally
        {
            lock (_jitterBuffer)
            {
                _jitterBuffer.Tick();
            }
        }
    }
}