using System;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientEntity(int id, VoiceCraftWorld world) : VoiceCraftEntity(id, world)
{
    private readonly CircularBuffer<byte> _outputBuffer = new(Constants.DecodeBufferBytes);
    private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
    private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);
    private readonly byte[] _encodedData = new byte[Constants.MaximumEncodedBytes];
    private readonly byte[] _readBuffer = new byte[Constants.BytesPerFrame];

    private long _startTick = Environment.TickCount64;
    private DateTime _lastPacket = DateTime.MinValue;
    private bool _isVisible;
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
            if (_userMuted == value) return;
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
        while (_startTick - currentTick < 0)
        {
            _startTick += Constants.FrameSizeMs;
            Array.Clear(_readBuffer);
            var read = Read(_readBuffer);
            if (read > 0)
                WriteToOutputBuffer(_readBuffer, Constants.BitDepth / 8 * Constants.Channels * read);
        }
    }

    public void ClearBuffer()
    {
        lock (_outputBuffer)
        lock (_jitterBuffer)
        {
            _outputBuffer.Clear();
            _jitterBuffer.Reset(); //Also reset the jitter buffer.
        }
    }

    public int Read(byte[] buffer, int count)
    {
        var read = ReadFromOutputBuffer(buffer, count);
        return read <= 0 ? 0 : read;
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

        lock (_jitterBuffer)
        {
            try
            {
                Array.Clear(_encodedData);
                var outPacket = new SpeexDSPJitterBufferPacket(_encodedData, (uint)_encodedData.Length);
                var startOffset = 0;
                if (_jitterBuffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(_encodedData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
            }
            catch
            {
                return 0;
            }
            finally
            {
                _jitterBuffer.Tick();
            }
        }
    }

    private void WriteToOutputBuffer(Span<byte> buffer, int count)
    {
        lock (_outputBuffer)
        {
            if (_outputBuffer.IsFull) return; //We have to drop the packet.
            for (var i = 0; i < count; i++)
                _outputBuffer.PushBack(buffer[i]);
        }
    }

    private int ReadFromOutputBuffer(Span<byte> buffer, int count)
    {
        lock (_outputBuffer)
        {
            var read = 0;
            for (var i = 0; i < count; i++)
            {
                if (_outputBuffer.IsEmpty) return read;
                buffer[i] = _outputBuffer.Front();
                _outputBuffer.PopFront();
                read++;
            }

            return read;
        }
    }
}