using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using LiteNetLib.Utils;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, IVisibleComponent, ISerializableEntityComponent
    {
        public static readonly QueryDescription Query = new QueryDescription().WithAll<AudioListenerComponent>();
        private string _environmentId = string.Empty;
        private ulong _bitmask; //Will change to a default value later.
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public ComponentType ComponentType => ComponentType.AudioListener;
        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public string EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value || !IsAlive) return;
                _environmentId = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value || !IsAlive) return;
                _bitmask = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public AudioListenerComponent(Entity entity)
        {
            if (entity.Has<AudioListenerComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public virtual int ReadInput(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        
        public void GetVisibleEntities(World world, List<Entity> entities)
        {
            world.Query(in AudioSourceComponent.Query, entity =>
            {
                var audioListenerComponent = entity.Get<AudioListenerComponent>();
                if (Entity == entity || entities.Contains(entity))
                    return; //Already checked entity or it's a local entity.
                
                var components = audioListenerComponent.Entity.GetAllComponents();

                foreach (var component in components)
                {
                    if (!(component is IVisibilityComponent visibilityComponent)) continue;
                    if (!visibilityComponent.VisibleTo(Entity)) return; //Not visible return the function.
                }
                
                if(world.Has<AudioListenerComponent>(entity))
                    world.Get<AudioListenerComponent>(entity).GetVisibleEntities(world, entities);
                if(world.Has<AudioSourceComponent>(entity))
                    world.Get<AudioSourceComponent>(entity).GetVisibleEntities(world, entities);
            });
        }
        
        public bool VisibleTo(Entity entity)
        {
            entity.TryGet<AudioSourceComponent>(out var audioSourceComponent);
            //Check for audioListener and check against the bitmask and environment ID.
            return (Bitmask & (audioSourceComponent?.Bitmask ?? 0)) != 0 && audioSourceComponent?.EnvironmentId == _environmentId;
        }
        
        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EnvironmentId);
            writer.Put(Bitmask);
        }

        public void Deserialize(NetDataReader reader)
        {
            EnvironmentId = reader.GetString();
            Bitmask = reader.GetULong();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<AudioListenerComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}