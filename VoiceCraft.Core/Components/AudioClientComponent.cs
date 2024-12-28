using System;
using System.Collections.Generic;
using Arch.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioClientComponent : IAudioInput, IAudioOutput, IComponent<AudioClientComponent>
    {
        private readonly VoiceCraftServerClient _client;
        private IAudioInput? _audioInput;
        
        public event Action<AudioClientComponent>? OnUpdate;
        public event Action<AudioClientComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }
        public IAudioInput? AudioInput
        {
            get => _audioInput;
            set
            {
                if (value == _audioInput) return;
                _audioInput = value;
                OnUpdate?.Invoke(this);
            }
        }

        public AudioClientComponent(VoiceCraftServerClient client, World world, Entity entity)
        {
            _client = client;
            World = world;
            Entity = entity;
            _client.OnDisconnected += OnDisconnected;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void GetVisibleEntities(List<Entity> entities)
        {
            AudioInput?.GetVisibleEntities(entities);
        }
        
        private void OnDisconnected()
        {
            Destroy();
        }
        
        public void Destroy()
        {
            _client.OnDisconnected -= OnDisconnected;
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}