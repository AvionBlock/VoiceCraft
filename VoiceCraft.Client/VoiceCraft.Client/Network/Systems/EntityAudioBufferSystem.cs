using System;
using System.Collections.Concurrent;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network.Systems
{
    public class EntityAudioBufferSystem
    {
        private readonly VoiceCraftClient _client;
        private readonly VoiceCraftWorld _world;
        private readonly ConcurrentDictionary<VoiceCraftEntity, EntityJitterBuffer> _entityJitterBuffers = new();

        public EntityAudioBufferSystem(VoiceCraftClient client)
        {
            _client = client;
            _world = _client.World;
            
            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }

        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            if (_entityJitterBuffers.ContainsKey(entity)) return;
            _entityJitterBuffers.TryAdd(entity, new EntityJitterBuffer(entity));
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            if(_entityJitterBuffers.TryRemove(entity, out var jitterBuffer)) return;
            jitterBuffer?.Dispose();
        }

        private class EntityJitterBuffer : IDisposable
        {
            private readonly VoiceCraftEntity _entity;
            private readonly SpeexDSPJitterBuffer _buffer;
            private readonly OpusDecoder _decoder;
            private readonly byte[] _bufferData = new byte[Constants.MaximumEncodedBytes];

            public EntityJitterBuffer(VoiceCraftEntity entity)
            {
                _entity = entity;
                _buffer = new SpeexDSPJitterBuffer(Constants.FrameSizeMs);
                _decoder = new OpusDecoder(Constants.SampleRate, Constants.Channels);
                _entity.OnAudioReceived += OnEntityAudioReceived;
            }

            public void Get(byte[] buffer)
            {
                if(buffer.Length <= Constants.BytesPerFrame)
                    throw new InvalidOperationException("Buffer is too small!");

                var outPacket = new SpeexDSPJitterBufferPacket(_bufferData, (uint)buffer.Length);
                var startOffset = 0;
                if (_buffer.Get(ref outPacket, Constants.BytesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                {
                    _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
                }
                else
                {
                    _decoder.Decode(_bufferData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
                }
                
                _buffer.Tick();
            }
            
            private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
            {
                var inPacket = new SpeexDSPJitterBufferPacket(data, (uint)data.Length)
                {
                    sequence = 0, //Don't care about the sequence.
                    span = Constants.FrameSizeMs,
                    timestamp = timestamp
                };
                
                _buffer.Put(ref inPacket);
            }

            public void Dispose()
            {
                _entity.OnAudioReceived -= OnEntityAudioReceived;
                _decoder.Dispose();
                _buffer.Dispose();
            }
        }
    }
}