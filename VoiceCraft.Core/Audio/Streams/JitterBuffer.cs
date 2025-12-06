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
using System;
using System.Linq;

namespace VoiceCraft.Core.Audio.Streams
{
    public class JitterBuffer
    {
        public WaveFormat WaveFormat { get; set; }
        
        // Optimized O(1) Count
        public int Count => _count;
        private int _count = 0;

        private bool IsFirst;
        private bool IsPreloaded;
        private Frame[] QueuedFrames;
        private int QueueLength;
        private int MaxQueueLength;
        private uint Sequence;

        private int SilencedFrames;
        
        // Lock object for internal synchronization if methods are called directly
        private readonly object _lock = new object();

        public JitterBuffer(WaveFormat WaveFormat, int bufferMilliseconds = 80, int maxBufferSizeMilliseconds = 1000, int frameSizeMS = 20)
        {
            this.WaveFormat = WaveFormat;
            QueueLength = bufferMilliseconds / frameSizeMS;
            MaxQueueLength = maxBufferSizeMilliseconds / frameSizeMS;
            QueuedFrames = new Frame[MaxQueueLength];
            IsFirst = true;
        }

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
                //Remove Old Frames.
                RemoveOldFrames();

                //Insert the packet if it's not lower than the current sequence.
                if (frame.Sequence > Sequence)
                {
                    //Find an empty slot.
                    var empty = FindEmptySlot();
                    if (empty != -1)
                    {
                        QueuedFrames[empty] = frame;
                        _count++;
                    }
                }
                else
                {
                    //We haven't found an empty slot so we replace the oldest/highest packet sequence.
                    var oldest = FindOldestSlot();
                    if (QueuedFrames[oldest].Buffer == null) _count++; // Should theoretically not happen if oldest slot logic assumes occupied
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
                        //This is the frame we expected
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
                        //Missed this frame, but the next queued one might have FEC info
                        Sequence++;
                        outFrame.Buffer = QueuedFrames[earliest].Buffer;
                        outFrame.Length = QueuedFrames[earliest].Length;
                        outFrame.Sequence = Sequence;
                        status = StatusCode.Missed;
                    }
                    else
                    {
                        //Missed this frame and we have no FEC data to work with
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
                Sequence++; // Always increment sequence on Get attempt
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
            if(QueuedFrames[0].Buffer != null) earliest = QueuedFrames[0].Sequence; // Fix initialization
            
            int index = -1;
            
            // First pass to find any valid frame to initialize comparison
            for (int i = 0; i < QueuedFrames.Length; i++)
            {
                if(QueuedFrames[i].Buffer != null)
                {
                     if(index == -1 || QueuedFrames[i].Sequence < earliest)
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
            //Remove Old Packets
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

    public class VoiceCraftJitterBuffer
    {
        public readonly int FrameSizeMS;
        private Frame inFrame;
        private Frame outFrame;
        private readonly JitterBuffer JitterBuffer;
        private readonly OpusDecoder Decoder;
        private readonly object _lock = new object();

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
                outFrame.Buffer = new byte[decodedFrame.Length]; // This buffer might be too large for small frames but fine for output
            }
            // Array.Clear not strictly necessary if Decoder overwrites, but checking bytesRead is safer.

            var bytesRead = 0;
            
            // JitterBuffer class is now thread-safe internally, but we lock here if we need atomicity between JitterBuffer and Decoder?
            // Actually, internal lock in JitterBuffer is enough for JitterBuffer state.
            // But we are reusing `outFrame` which is a field of THIS class.
            // Multi-threaded access to `Get` uses `outFrame` field -> Race condition on `outFrame`.
            
            lock (_lock)
            {
                var status = JitterBuffer.Get(ref outFrame); // JitterBuffer handles its own locking.
                
                if (status == StatusCode.Success)
                {
                    bytesRead = Decoder.Decode(outFrame.Buffer, outFrame.Length, decodedFrame, decodedFrame.Length);
                }
                else if(status == StatusCode.NotReady)
                {
                    return 0;
                }
                else
                {
                    // PLC (Packet Loss Concealment)
                    bytesRead = Decoder.Decode(null, 0, decodedFrame, decodedFrame.Length);
                }
            }

            return bytesRead;
        }

        public void Put(byte[] data, uint packetCount)
        {
            // Lock to protect `inFrame` field usage
            lock(_lock)
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
}
