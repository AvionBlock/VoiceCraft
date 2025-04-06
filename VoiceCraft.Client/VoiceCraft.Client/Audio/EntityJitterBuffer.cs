using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Audio
{
    public class EntityJitterBuffer : IDisposable, IWaveProvider
    {
        public WaveFormat WaveFormat { get; }

        private readonly VoiceCraftEntity _entity;
        private readonly SpeexDSPJitterBuffer _buffer;
        private readonly BufferedWaveProvider _bufferedAudio;
        private readonly OpusDecoder _decoder;
        private readonly CancellationTokenSource _cts;
        private readonly Task _decodeThread;

        private readonly byte[] _decodeData = new byte[Constants.MaximumEncodedBytes];
        private readonly byte[] _decodeBuffer = new byte[Constants.BytesPerFrame];
        private DateTime _lastPacket = DateTime.MinValue;

        public EntityJitterBuffer(WaveFormat waveFormat, VoiceCraftEntity entity)
        {
            WaveFormat = waveFormat;
            _entity = entity;
            _entity.OnAudioReceived += OnEntityAudioReceived;

            _buffer = new SpeexDSPJitterBuffer(Constants.SamplesPerFrame);
            _bufferedAudio = new BufferedWaveProvider(WaveFormat)
                { ReadFully = true, DiscardOnBufferOverflow = true, BufferDuration = TimeSpan.FromMilliseconds(Constants.BufferDurationMs) };
            _decoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
            _cts = new CancellationTokenSource();
            _decodeThread = StartJitterThread();
        }

        public int Read(byte[] buffer, int offset, int count) => _bufferedAudio.Read(buffer, offset, count);

        private void DecodeNext()
        {
            Array.Clear(_decodeData);
            Array.Clear(_decodeBuffer);

            var outPacket = new SpeexDSPJitterBufferPacket(_decodeData, (uint)_decodeData.Length);
            var startOffset = 0;
            var addSamples = (_lastPacket - DateTime.UtcNow).TotalMilliseconds < Constants.SilenceThresholdMs;
            
            if (_buffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) == JitterBufferState.JITTER_BUFFER_OK)
            {
                _decoder.Decode(_decodeData, (int)outPacket.len, _decodeBuffer, Constants.SamplesPerFrame, false);
                _lastPacket = outPacket.user_data == 1 ? DateTime.MinValue : DateTime.UtcNow;
                _bufferedAudio.AddSamples(_decodeBuffer, 0, _decodeBuffer.Length);
            }
            else
            {
                _decoder.Decode(null, 0, _decodeBuffer, Constants.SamplesPerFrame, false);
                _bufferedAudio.AddSamples(_decodeBuffer, 0, _decodeBuffer.Length);
            }
            
            _buffer.Tick();
        }

        private void OnEntityAudioReceived(byte[] data, uint timestamp, bool endOfTransmission, VoiceCraftEntity entity)
        {
            var inPacket = new SpeexDSPJitterBufferPacket(data, (uint)data.Length)
            {
                sequence = 0, //Don't care about the sequence.
                span = Constants.SamplesPerFrame,
                timestamp = timestamp,
                user_data = endOfTransmission ? 1u : 0u
            };
            _buffer.Put(ref inPacket);
        }

        private Task StartJitterThread()
        {
            return Task.Run(async () =>
            {
                var startTick = Environment.TickCount64;
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var tick = Environment.TickCount64;
                        var dist = startTick - tick;
                        if (dist > 0)
                        {
                            await Task.Delay((int)dist).ConfigureAwait(false);
                            continue;
                        }

                        startTick += Constants.FrameSizeMs;
                        DecodeNext();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            _entity.OnAudioReceived -= OnEntityAudioReceived;
            _cts.Cancel();
            _decodeThread.Wait();
            _cts.Dispose();
            _decoder.Dispose();
            _buffer.Dispose();
            _bufferedAudio.ReadFully = false; //Makes it so it auto removes from the mixers.
            _bufferedAudio.ClearBuffer();
            GC.SuppressFinalize(this);
        }
    }
}