using System;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Effects
{
    public class ProximityEffectComponent : IAudioEffect, IComponent<ProximityEffectComponent>
    {
        private string? _environmentId;
        private uint _minRange;
        private uint _maxRange;

        public event Action<ProximityEffectComponent>? OnUpdate;
        public event Action<ProximityEffectComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }

        public string? EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value) return;
                _environmentId = value;
                OnUpdate?.Invoke(this);
            }
        }

        public uint MinRange
        {
            get => _minRange;
            set
            {
                if (_minRange == value) return;
                _minRange = value;
                OnUpdate?.Invoke(this);
            }
        }

        public uint MaxRange
        {
            get => _maxRange;
            set
            {
                if (_maxRange == value) return;
                _maxRange = value;
                OnUpdate?.Invoke(this);
            }
        }

        public EffectBitmask Bitmask => EffectBitmask.ProximityVolume;

        public ProximityEffectComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public bool CanSeeEntity(Entity entity)
        {
            //Is the same entity or doesn't have any transform component on either entity, should see it.
            if (entity == Entity || !entity.Has<TransformComponent>() || !Entity.Has<TransformComponent>())
                return true;

            entity.TryGet<AudioBitmaskComponent>(out var bitmaskComponent);
            Entity.TryGet<AudioBitmaskComponent>(out var selfBitmaskComponent);
            var combinedBitmask = bitmaskComponent?.Bitmask ?? 0 | selfBitmaskComponent?.Bitmask ?? 0;
            var enabledEffects = bitmaskComponent?.GetEnabledBitmaskEffects(combinedBitmask) ??
                                 0 | selfBitmaskComponent?.GetEnabledBitmaskEffects(combinedBitmask) ?? 0;
            if ((enabledEffects & (ulong)Bitmask) == 0) return true; //Effect not enabled via bitmask, can see entity/true.

            entity.TryGet<ProximityEffectComponent>(out var proximityEffectComponent); //Proximity effect on other entity.
            //Get distance between entities.
            var distance = Vector3.Distance(entity.Get<TransformComponent>().Position, Entity.Get<TransformComponent>().Position);

            //If distance of entities is lower than or equal to max range and environment ID is equal and not null or whitespace, then true. else false.
            return distance <= _maxRange && !string.IsNullOrWhiteSpace(_environmentId) && proximityEffectComponent?.EnvironmentId == _environmentId;
        }

        public void Process(byte[] buffer)
        {
            throw new NotSupportedException();
        }

        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}