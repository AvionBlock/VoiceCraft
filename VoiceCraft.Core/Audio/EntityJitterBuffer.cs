using System;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;

namespace VoiceCraft.Core.Audio
{
    public class EntityJitterBuffer : IDisposable
    {
        private readonly VoiceCraftEntity _entity;
        private readonly SpeexDSPJitterBuffer _buffer;
        private readonly OpusDecoder _decoder;

        private readonly byte[] _decodeData = new byte[Constants.MaximumEncodedBytes];
        private readonly float[] _decodeBuffer = new float[Constants.FloatsPerFrame];
        private DateTime _lastPacket = DateTime.MinValue;
        private bool _receiving;

        public EntityJitterBuffer(VoiceCraftEntity entity)
        {
            _entity = entity;
            _entity.OnAudioReceived += OnEntityAudioReceived;

            _buffer = new SpeexDSPJitterBuffer(Constants.SamplesPerFrame);
            _decoder = new OpusDecoder(Constants.SampleRate, Constants.Channels);
        }

        private void DecodeNext()
        {
            Array.Clear(_decodeData, 0, _decodeData.Length);
            Array.Clear(_decodeBuffer, 0, _decodeBuffer.Length);

            var outPacket = new SpeexDSPJitterBufferPacket(_decodeData, (uint)_decodeData.Length);
            var startOffset = 0;
            
            if (_buffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) == JitterBufferState.JITTER_BUFFER_OK)
            {
                if (_receiving == false)
                {
                    //Just to smooth out audio.
                    _decoder.Decode(null, 0, _decodeBuffer, Constants.SamplesPerFrame, false);
                    _receiving = true;
                }
                
                _decoder.Decode(_decodeData, (int)outPacket.len, _decodeBuffer, Constants.SamplesPerFrame, false);
                _lastPacket = DateTime.UtcNow;
            }
            else
            {
                if ((DateTime.UtcNow - _lastPacket).TotalMilliseconds < Constants.SilenceThresholdMs)
                {
                    _decoder.Decode(null, 0, _decodeBuffer, Constants.SamplesPerFrame, false);
                }
                else
                {
                    _receiving = false;
                }
            }
            
            _buffer.Tick();
        }

        private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
        {
            var inPacket = new SpeexDSPJitterBufferPacket(data, (uint)data.Length)
            {
                sequence = 0, //Don't care about the sequence.
                span = Constants.SamplesPerFrame,
                timestamp = timestamp
            };
            _buffer.Put(ref inPacket);
        }

        public void Dispose()
        {
            _entity.OnAudioReceived -= OnEntityAudioReceived;
            _decoder.Dispose();
            _buffer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}