using System;
using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Audio;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network.Systems
{
    public class EntityAudioSystem : IDisposable, ISampleProvider
    {
        public WaveFormat WaveFormat { get; }

        private readonly WaveFormat _recordFormat;
        private readonly VoiceCraftWorld _world;
        private readonly ConcurrentDictionary<VoiceCraftEntity, EntityJitterBuffer> _entityJitterBuffers = new();
        private readonly MixingSampleProvider _mixer;

        public EntityAudioSystem(WaveFormat audioFormat, WaveFormat recordFormat, VoiceCraftClient client)
        {
            WaveFormat = audioFormat;
            
            _recordFormat = recordFormat;
            _world = client.World;
            _mixer = new MixingSampleProvider(VoiceCraftClient.AudioWaveFormat) { ReadFully = true };

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }

        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            if (_entityJitterBuffers.ContainsKey(entity)) return;
            var entityJitterBuffer = new EntityJitterBuffer(_recordFormat, entity);
            _entityJitterBuffers.TryAdd(entity, entityJitterBuffer);
            _mixer.AddMixerInput(new Wave16ToFloatProvider(entityJitterBuffer));
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            if (_entityJitterBuffers.TryRemove(entity, out var jitterBuffer)) return;
            jitterBuffer?.Dispose(); //Should auto remove from the mixer.
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

        public int Read(float[] buffer, int offset, int count)
        {
            return _mixer.Read(buffer, offset, count);
        }
    }
}