////////////////////////////////////////////////////////////////////////////////////////
// MIT License                                                                        //
//                                                                                    //
// Copyright (c) [10/10/2025] [SineVector241]                                         //
//                                                                                    //
// Permission is hereby granted, free of charge, to any person obtaining a copy       //
// of this software and associated documentation files (the "Software"), to deal      //
// in the Software without restriction, including without limitation the rights       //
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell          //
// copies of the Software, and to permit persons to whom the Software is              //
// furnished to do so, subject to the following conditions:                           //
//                                                                                    //
// The above copyright notice and this permission notice shall be included in all     //
// copies or substantial portions of the Software.                                    //
//                                                                                    //
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR         //
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,           //
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE        //
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER             //
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,      //
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE      //
// SOFTWARE.                                                                          //
////////////////////////////////////////////////////////////////////////////////////////

using NAudio.Wave;
using OpusSharp.Core;

namespace VoiceCraft.Core.Audio.Streams;

/// <summary>
/// Jitter buffer for handling out-of-order and delayed audio packets.
/// Thread-safe implementation with optimized frame management.
/// </summary>
public class JitterBuffer
{
    /// <summary>
    /// Gets or sets the wave format.
    /// </summary>
    public WaveFormat WaveFormat { get; set; }

    /// <summary>
    /// Gets the current frame count in the buffer.
    /// </summary>
    public int Count => _count;

    private int _count;
    private bool IsFirst;
    private bool IsPreloaded;
    private Frame[] QueuedFrames;
    private int QueueLength;
    private int MaxQueueLength;
    private uint Sequence;
    private int SilencedFrames;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JitterBuffer"/> class.
    /// </summary>
    public JitterBuffer(WaveFormat waveFormat, int bufferMilliseconds = 80, int maxBufferSizeMilliseconds = 1000, int frameSizeMS = 20)
    {
        WaveFormat = waveFormat;
        QueueLength = bufferMilliseconds / frameSizeMS;
        MaxQueueLength = maxBufferSizeMilliseconds / frameSizeMS;
        QueuedFrames = new Frame[MaxQueueLength];
        IsFirst = true;
    }

    /// <summary>
    /// Adds a frame to the jitter buffer.
    /// </summary>
    public void Put(Frame frame)
    {
        lock (_lock)
        {
            if (IsFirst)
            {
                QueuedFrames[0] = frame;
                _count = 1;
                Sequence = frame.Sequence;
                SilencedFrames = 0;
                IsFirst = false;
                return;
            }

            RemoveOldFrames();

            if (frame.Sequence > Sequence)
            {
                var empty = FindEmptySlot();
                if (empty != -1)
                {
                    QueuedFrames[empty] = frame;
                    _count++;
                }
            }
            else
            {
                var oldest = FindOldestSlot();
                if (QueuedFrames[oldest].Buffer == null) _count++;
                QueuedFrames[oldest] = frame;
            }

            if (!IsPreloaded && _count >= QueueLength)
            {
                IsPreloaded = true;
            }
        }
    }

    public StatusCode Get(ref Frame outFrame)
    {
        lock (_lock)
        {
            var status = StatusCode.Failed;
            if (!IsPreloaded)
                return StatusCode.NotReady;

            RemoveOldFrames();

            var earliest = FindEarliestSlot();
            if (earliest != -1)
            {
                uint earliestSequence = QueuedFrames[earliest].Sequence;
                uint currentSequence = Sequence;
                var distance = earliestSequence - currentSequence;

                if (distance == 0 || IsFirst)
                {
                    Sequence = QueuedFrames[earliest].Sequence;
                    IsFirst = false;
                    SilencedFrames = 0;

                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Length = QueuedFrames[earliest].Length;
                    outFrame.Sequence = QueuedFrames[earliest].Sequence;

                    QueuedFrames[earliest].Buffer = null;
                    _count--;

                    status = StatusCode.Success;
                }
                else if (distance == 1)
                {
                    Sequence++;
                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Length = QueuedFrames[earliest].Length;
                    outFrame.Sequence = Sequence;
                    status = StatusCode.Missed;
                }
                else
                {
                    Sequence++;
                    status = StatusCode.Failed;
                }
            }
            else if (!IsFirst)
            {
                if (SilencedFrames < 5)
                    SilencedFrames++;
                else
                {
                    IsFirst = true;
                    IsPreloaded = false;
                }
            }
            Sequence++;
            return status;
        }
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < QueuedFrames.Length; i++)
        {
            if (QueuedFrames[i].Buffer == null)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindOldestSlot()
    {
        int index = 0;
        uint oldest = QueuedFrames[index].Sequence;
        for (int i = 1; i < QueuedFrames.Length; i++)
        {
            if (QueuedFrames[i].Sequence > oldest)
            {
                oldest = QueuedFrames[i].Sequence;
                index = i;
            }
        }
        return index;
    }

    private int FindEarliestSlot()
    {
        uint earliest = uint.MaxValue;
        if (QueuedFrames[0].Buffer != null) earliest = QueuedFrames[0].Sequence;

        int index = -1;

        for (int i = 0; i < QueuedFrames.Length; i++)
        {
            if (QueuedFrames[i].Buffer != null)
            {
                if (index == -1 || QueuedFrames[i].Sequence < earliest)
                {
                    earliest = QueuedFrames[i].Sequence;
                    index = i;
                }
            }
        }

        return index;
    }

    private void RemoveOldFrames()
    {
        for (int i = 0; i < QueuedFrames.Length; i++)
        {
            if (QueuedFrames[i].Buffer != null && QueuedFrames[i].Sequence < Sequence)
            {
                QueuedFrames[i].Buffer = null;
                _count--;
            }
        }
    }
}

/// <summary>
/// VoiceCraft-specific jitter buffer with Opus decoding.
/// </summary>
public class VoiceCraftJitterBuffer
{
    public readonly int FrameSizeMS;
    private Frame inFrame;
    private Frame outFrame;
    private readonly JitterBuffer JitterBuffer;
    private readonly OpusDecoder Decoder;
    private readonly object _lock = new();

    public VoiceCraftJitterBuffer(WaveFormat waveFormat, int frameSizeMS = 20, int jitterBufferSize = 80)
    {
        FrameSizeMS = frameSizeMS;
        JitterBuffer = new JitterBuffer(waveFormat, jitterBufferSize, 2000, frameSizeMS);
        Decoder = new OpusDecoder(waveFormat.SampleRate, waveFormat.Channels);
    }

    public int Get(byte[] decodedFrame)
    {
        if (outFrame.Buffer == null)
        {
            outFrame.Buffer = new byte[decodedFrame.Length];
        }

        var bytesRead = 0;

        lock (_lock)
        {
            var status = JitterBuffer.Get(ref outFrame);

            if (status == StatusCode.Success)
            {
                bytesRead = Decoder.Decode(outFrame.Buffer, outFrame.Length, decodedFrame, decodedFrame.Length);
            }
            else if (status == StatusCode.NotReady)
            {
                return 0;
            }
            else
            {
                bytesRead = Decoder.Decode(null, 0, decodedFrame, decodedFrame.Length);
            }
        }

        return bytesRead;
    }

    public void Put(byte[] data, uint packetCount)
    {
        lock (_lock)
        {
            inFrame.Buffer = data;
            inFrame.Length = data.Length;
            inFrame.Sequence = packetCount;
            JitterBuffer.Put(inFrame);
        }
    }
}

public struct Frame
{
    public byte[]? Buffer;
    public int Length;
    public uint Sequence;
}

public enum StatusCode
{
    NotReady = -2,
    Failed = -1,
    Success = 0,
    Missed = 1
}
