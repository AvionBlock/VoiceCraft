using NAudio.Wave;
using OpusSharp.Core;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace VoiceCraft.Core.Audio.Streams;

/// <summary>
/// Jitter buffer for handling out-of-order and delayed audio packets.
/// Thread-safe implementation with optimized frame management using Memory&lt;byte&gt;.
/// </summary>
public class JitterBuffer
{
    private readonly object _lock = new();
    private readonly int _queueLength;
    private readonly Frame[] _queuedFrames;

    private int _count;
    private bool _isFirst = true;
    private bool _isPreloaded;
    private uint _sequence;
    private int _silencedFrames;

    /// <summary>
    /// Gets the wave format.
    /// </summary>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Gets the current frame count in the buffer.
    /// </summary>
    public int Count
    {
        get { lock (_lock) return _count; }
    }

    public JitterBuffer(WaveFormat waveFormat, int bufferMilliseconds = 80, int maxBufferSizeMilliseconds = 1000, int frameSizeMS = 20)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        WaveFormat = waveFormat;
        _queueLength = bufferMilliseconds / frameSizeMS;
        _queuedFrames = new Frame[maxBufferSizeMilliseconds / frameSizeMS];
    }

    /// <summary>
    /// Adds a frame to the jitter buffer.
    /// </summary>
    public void Put(Frame frame)
    {
        lock (_lock)
        {
            if (_isFirst)
            {
                _queuedFrames[0] = frame;
                _count = 1;
                _sequence = frame.Sequence;
                _silencedFrames = 0;
                _isFirst = false;
                return;
            }

            RemoveOldFrames();

            if (frame.Sequence > _sequence)
            {
                var empty = FindEmptySlot();
                if (empty != -1)
                {
                    _queuedFrames[empty] = frame;
                    _count++;
                }
            }
            else
            {
                var oldest = FindOldestSlot();
                if (_queuedFrames[oldest].Buffer.IsEmpty) _count++;
                _queuedFrames[oldest] = frame;
            }

            if (!_isPreloaded && _count >= _queueLength)
            {
                _isPreloaded = true;
            }
        }
    }

    public StatusCode Get(ref Frame outFrame)
    {
        lock (_lock)
        {
            if (!_isPreloaded)
                return StatusCode.NotReady;

            RemoveOldFrames();

            var earliest = FindEarliestSlot();
            if (earliest != -1)
            {
                uint earliestSequence = _queuedFrames[earliest].Sequence;
                uint currentSequence = _sequence;
                var distance = earliestSequence - currentSequence;

                if (distance == 0 || _isFirst)
                {
                    _sequence = _queuedFrames[earliest].Sequence;
                    _isFirst = false;
                    _silencedFrames = 0;

                    outFrame = _queuedFrames[earliest];

                    // Clear the slot
                    _queuedFrames[earliest] = default;
                    _count--;

                    return StatusCode.Success;
                }
                
                if (distance == 1)
                {
                    _sequence++;
                    outFrame = _queuedFrames[earliest];
                    outFrame.Sequence = _sequence; // Force sequence match for "Missed" logic? 
                    
                    return StatusCode.Missed; 
                }

                // Gap too large
                _sequence++;
                return StatusCode.Failed;
            }
            
            if (!_isFirst)
            {
                if (_silencedFrames < 5)
                    _silencedFrames++;
                else
                {
                    _isFirst = true;
                    _isPreloaded = false;
                }
            }
            _sequence++;
            return StatusCode.Failed;
        }
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < _queuedFrames.Length; i++)
        {
            if (_queuedFrames[i].Buffer.IsEmpty)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindOldestSlot()
    {
        int index = 0;
        uint oldest = _queuedFrames[index].Sequence;
        for (int i = 1; i < _queuedFrames.Length; i++)
        {
            if (_queuedFrames[i].Sequence > oldest)
            {
                oldest = _queuedFrames[i].Sequence;
                index = i;
            }
        }
        return index;
    }

    private int FindEarliestSlot()
    {
        uint earliest = uint.MaxValue;
        if (!_queuedFrames[0].Buffer.IsEmpty) earliest = _queuedFrames[0].Sequence;

        int index = -1;

        for (int i = 0; i < _queuedFrames.Length; i++)
        {
            if (!_queuedFrames[i].Buffer.IsEmpty)
            {
                if (index == -1 || _queuedFrames[i].Sequence < earliest)
                {
                    earliest = _queuedFrames[i].Sequence;
                    index = i;
                }
            }
        }

        return index;
    }

    private void RemoveOldFrames()
    {
        for (int i = 0; i < _queuedFrames.Length; i++)
        {
            if (!_queuedFrames[i].Buffer.IsEmpty && _queuedFrames[i].Sequence < _sequence)
            {
                _queuedFrames[i] = default;
                _count--;
            }
        }
    }
    
    public Frame GetFrame(int index) => _queuedFrames[index];
}

/// <summary>
/// VoiceCraft-specific jitter buffer with Opus decoding.
/// </summary>
public sealed class VoiceCraftJitterBuffer : IDisposable
{
    private readonly int _frameSizeMS;
    private readonly JitterBuffer _jitterBuffer;
    private readonly OpusDecoder _decoder;
    private readonly object _lock = new();

    private Frame _inFrame;
    private Frame _outFrame;
    private bool _disposed;

    /// <summary>
    /// Gets the frame size in milliseconds.
    /// </summary>
    public int FrameSizeMS => _frameSizeMS;

    public VoiceCraftJitterBuffer(WaveFormat waveFormat, int frameSizeMS = 20, int jitterBufferSize = 80)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        _frameSizeMS = frameSizeMS;
        _jitterBuffer = new JitterBuffer(waveFormat, jitterBufferSize, 2000, frameSizeMS);
        _decoder = new OpusDecoder(waveFormat.SampleRate, waveFormat.Channels);
    }

    public int Get(Span<byte> decodedFrame)
    {
        lock (_lock)
        {
            if (_disposed) return 0;

            var status = _jitterBuffer.Get(ref _outFrame);

            if (status == StatusCode.Success)
            {
                if (MemoryMarshal.TryGetArray(_outFrame.Buffer, out ArraySegment<byte> segment))
                {
                    byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(decodedFrame.Length);
                    try
                    {
                        int read = _decoder.Decode(segment.Array, segment.Count, outputBuffer, outputBuffer.Length);
                        new ReadOnlySpan<byte>(outputBuffer, 0, read).CopyTo(decodedFrame);
                        return read;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(outputBuffer);
                    }
                }
            }
            else if (status == StatusCode.NotReady)
            {
                return 0;
            }
            
            // Packet Loss / Concealment
            byte[] concealmentBuffer = ArrayPool<byte>.Shared.Rent(decodedFrame.Length);
            try
            {
                int read = _decoder.Decode(null, 0, concealmentBuffer, concealmentBuffer.Length);
                new ReadOnlySpan<byte>(concealmentBuffer, 0, read).CopyTo(decodedFrame);
                return read;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(concealmentBuffer);
            }
        }
    }

    public void Put(ReadOnlySpan<byte> data, uint packetCount)
    {
        lock (_lock)
        {
            if (_disposed) return;
            
            byte[] storage = new byte[data.Length];
            data.CopyTo(storage);
            
            _inFrame.Buffer = new Memory<byte>(storage);
            _inFrame.Length = data.Length;
            _inFrame.Sequence = packetCount;
            
            _jitterBuffer.Put(_inFrame);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _decoder?.Dispose();
            _disposed = true;
        }
    }
}

public struct Frame : IEquatable<Frame>
{
    public Memory<byte> Buffer { get; set; }
    public int Length { get; set; }
    public uint Sequence { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is Frame frame && Equals(frame);
    }

    public bool Equals(Frame other)
    {
        return Buffer.Equals(other.Buffer) &&
               Length == other.Length &&
               Sequence == other.Sequence;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Buffer, Length, Sequence);
    }

    public static bool operator ==(Frame left, Frame right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Frame left, Frame right)
    {
        return !(left == right);
    }
}

public enum StatusCode
{
    NotReady = -2,
    Failed = -1,
    Success = 0,
    Missed = 1
}
