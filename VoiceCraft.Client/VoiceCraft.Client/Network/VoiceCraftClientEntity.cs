using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NAudio.Wave;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClientEntity : VoiceCraftEntity
    {
        public bool IsVisible { get; set; }

        private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);
        private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
        private readonly byte[] _encodedData = new byte[Constants.MaximumEncodedBytes];
        private DateTime _lastPacket = DateTime.MinValue;

        public VoiceCraftClientEntity(int id) : base(id)
        {
            StartJitterThread();
        }

        private readonly BufferedWaveProvider _outputBuffer = new(new WaveFormat(Constants.SampleRate, Constants.Channels))
        {
            ReadFully = false,
            DiscardOnBufferOverflow = true
        };

        public int Read(byte[] buffer, int offset, int count)
        {
            return _outputBuffer.Read(buffer, offset, count);
        }

        public override void ReceiveAudio(byte[] buffer, uint timestamp, float frameLoudness)
        {
            var inPacket = new SpeexDSPJitterBufferPacket(buffer, (uint)buffer.Length)
            {
                timestamp = timestamp,
                span = Constants.SamplesPerFrame
            };
            _jitterBuffer.Put(ref inPacket);
            base.ReceiveAudio(buffer, timestamp, frameLoudness);
        }

        public override void Destroy()
        {
            _jitterBuffer.Dispose();
            _decoder.Dispose();
            base.Destroy();
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

        private void StartJitterThread()
        {
            Task.Run(async () =>
            {
                var startTick = Environment.TickCount64;
                var readBuffer = new byte[Constants.BytesPerFrame];
                while (!Destroyed)
                {
                    try
                    {
                        var tick = Environment.TickCount;
                        var dist = startTick - tick;
                        if (dist > 0)
                        {
                            await Task.Delay((int)dist).ConfigureAwait(false);
                            continue;
                        }

                        startTick += Constants.FrameSizeMs;
                        Array.Clear(readBuffer);
                        var read = Read(readBuffer);
                        if (read > 0)
                            _outputBuffer.AddSamples(readBuffer, 0, readBuffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            });
        }
    }
}