using System;
using System.Collections.Concurrent;
using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network.Systems
{
    public class EntityAudioBufferSystem : IDisposable
    {
        private readonly VoiceCraftWorld _world;
        private readonly ConcurrentDictionary<VoiceCraftEntity, EntityJitterBuffer> _entityJitterBuffers = new();

        public EntityAudioBufferSystem(VoiceCraftClient client)
        {
            _world = client.World;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }

        public bool GetNextFrame(VoiceCraftEntity entity, byte[] buffer)
        {
            try
            {
                if (!_entityJitterBuffers.TryGetValue(entity, out var jitterBuffer)) return false;
                jitterBuffer.Get(buffer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            if (_entityJitterBuffers.ContainsKey(entity)) return;
            _entityJitterBuffers.TryAdd(entity, new EntityJitterBuffer(entity));
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            if (_entityJitterBuffers.TryRemove(entity, out var jitterBuffer)) return;
            jitterBuffer?.Dispose();
        }

        public void Dispose()
        {
            _world.OnEntityCreated -= OnEntityCreated;
            _world.OnEntityDestroyed -= OnEntityDestroyed;

            foreach (var entity in _entityJitterBuffers)
            {
                entity.Value.Dispose(); //DISPOSE EVERYTHING!
            }

            _entityJitterBuffers.Clear();
            GC.SuppressFinalize(this);
        }

        private class EntityJitterBuffer : IDisposable
        {
            private readonly VoiceCraftEntity _entity;
            private readonly SpeexDSPJitterBuffer _buffer;
            private readonly OpusDecoder _decoder;
            private readonly byte[] _decodeData = new byte[Constants.MaximumEncodedBytes];

            public EntityJitterBuffer(VoiceCraftEntity entity)
            {
                _entity = entity;
                _buffer = new SpeexDSPJitterBuffer(Constants.SamplesPerFrame);
                _decoder = new OpusDecoder(Constants.SampleRate, Constants.Channels);
                _entity.OnAudioReceived += OnEntityAudioReceived;
            }

            public void Get(byte[] buffer)
            {
                if (buffer.Length < Constants.BytesPerFrame)
                    throw new InvalidOperationException("Buffer is too small!");

                Array.Clear(_decodeData);
                var outPacket = new SpeexDSPJitterBufferPacket(_decodeData, (uint)_decodeData.Length);
                var startOffset = 0;
                if (_buffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) != JitterBufferState.JITTER_BUFFER_OK)
                {
                    _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
                }
                else
                {
                    _decoder.Decode(_decodeData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false);
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
            }
        }
    }
}