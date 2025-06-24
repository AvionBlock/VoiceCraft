using System;
using System.Buffers;
using System.Threading.Tasks;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientEntity : VoiceCraftEntity
{
    private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
    private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);
    
    private CircularBuffer<short> _outputBuffer = new(Constants.OutputBufferShorts);
    private DateTime _lastPacket = DateTime.MinValue;
    private bool _isReading;
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
    public event Action<VoiceCraftClientEntity>? OnStartedSpeaking;
    public event Action<VoiceCraftClientEntity>? OnStoppedSpeaking;

    public VoiceCraftClientEntity(int id, VoiceCraftWorld world) : base(id, world)
    {
        Task.Run(ReaderLogic);
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

    public int Read(Span<short> buffer, int count)
    {
        if (_userMuted)
        {
            _isReading = false;
            return 0;
        }

        var read = ReadFromOutputBuffer(buffer, count);
        if (read <= 0)
        {
            ResizeDecodeBufferIfNeeded(count);
            if (!_isReading) return 0;
            _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
            _isReading = false;
            OnStoppedSpeaking?.Invoke(this);
            return 0;
        }

        if (_isReading) return read;
        OnStartedSpeaking?.Invoke(this);
        _isReading = true;
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
        lock (_decoder)
        lock (_jitterBuffer)
        {
            _jitterBuffer.Dispose();
            _decoder.Dispose();
        }

        base.Destroy();

        OnIsVisibleUpdated = null;
        OnVolumeUpdated = null;
        OnUserMutedUpdated = null;
        OnStartedSpeaking = null;
        OnStoppedSpeaking = null;
    }

    private void WriteToOutputBuffer(Span<short> buffer, int count)
    {
        lock (_outputBuffer)
        {
            if (_outputBuffer.IsFull) return; //We have to drop the packet.
            for (var i = 0; i < count; i++)
                _outputBuffer.PushBack(buffer[i]);
        }
    }

    private int ReadFromOutputBuffer(Span<short> buffer, int count)
    {
        lock (_outputBuffer)
        {
            var read = 0;
            for (var i = 0; i < count; i++)
            {
                if (_outputBuffer.IsEmpty || _outputBuffer.Size < count && !_isReading) return read;
                buffer[i] = _outputBuffer.Front();
                _outputBuffer.PopFront();
                read++;
            }

            return read;
        }
    }

    private void ResizeDecodeBufferIfNeeded(int newSize)
    {
        lock (_outputBuffer)
        {
            if (_outputBuffer.Capacity >= newSize) return;
            _outputBuffer = new CircularBuffer<short>(newSize, _outputBuffer.ToArray());
        }
    }

    private int GetNextPacket(Span<short> buffer)
    {
        if (buffer.Length * sizeof(short) < Constants.BytesPerFrame)
            return 0;

        var encodeBuffer = ArrayPool<byte>.Shared.Rent(Constants.MaximumEncodedBytes);
        lock (_jitterBuffer)
        {
            try
            {
                Array.Clear(encodeBuffer); //Clear the buffer.
                var outPacket = new SpeexDSPJitterBufferPacket(encodeBuffer, (uint)encodeBuffer.Length);
                var startOffset = 0;
                if (_jitterBuffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                    return (DateTime.UtcNow - _lastPacket).TotalMilliseconds > Constants.SilenceThresholdMs
                        ? 0
                        : _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);

                _lastPacket = DateTime.UtcNow;
                return _decoder.Decode(encodeBuffer, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
            }
            catch
            {
                return 0;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(encodeBuffer);
                _jitterBuffer.Tick();
            }
        }
    }

    private async Task ReaderLogic()
    {
        var startTick = Environment.TickCount64;
        var readBuffer = new short[Constants.BytesPerFrame / sizeof(short)];
        while (!Destroyed)
        {
            try
            {
                var tick = Environment.TickCount;
                var dist = startTick - tick;
                if (dist > 0)
                {
                    await Task.Delay((int)dist).ConfigureAwait(false); //Delay by required amount.
                    continue;
                }

                startTick += Constants.FrameSizeMs; //Step Forwards.
                Array.Clear(readBuffer); //Clear Read Buffer.
                var read = GetNextPacket(readBuffer);
                if (read <= 0) continue;
                
                WriteToOutputBuffer(readBuffer, Constants.BitDepth / 16 * Constants.Channels * read);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}