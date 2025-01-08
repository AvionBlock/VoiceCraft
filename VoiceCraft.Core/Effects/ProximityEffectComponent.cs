using System;
using System.Linq;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Effects
{
    public class ProximityEffectComponent : IAudioEffect, IComponent
    {
        private string? _environmentId;
        private uint _minRange;
        private uint _maxRange;

        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
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

        public EffectBitmask Bitmask => EffectBitmask.ProximityEffect;

        public ProximityEffectComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public bool IsVisibleToEntity(Entity otherEntity)
        {
            //Is the same entity or doesn't have any transform component on either entity, should see it.
            if (otherEntity == Entity || !otherEntity.Has<TransformComponent>() || !Entity.Has<TransformComponent>() || !IsBitmaskEnabled(otherEntity))
                return true;
            
            otherEntity.TryGet<ProximityEffectComponent>(out var otherEntityProximityEffectComponent);
            var entityTransformComponent = Entity.Get<TransformComponent>();
            var otherEntityTransformComponent = otherEntity.Get<TransformComponent>();
            var maxRange = Math.Max(_maxRange, otherEntityProximityEffectComponent?.MaxRange ?? 0);
            return Vector3.Distance(entityTransformComponent.Position, otherEntityTransformComponent.Position) <= maxRange;
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

        private bool IsBitmaskEnabled(Entity otherEntity)
        {
            if (!otherEntity.Has<AudioBitmaskComponent>() && !Entity.Has<AudioBitmaskComponent>())
                return false;

            Entity.TryGet<AudioBitmaskComponent>(out var entityBitmaskComponent);
            otherEntity.TryGet<AudioBitmaskComponent>(out var otherEntityBitmaskComponent);
            var combinedBitmask = ulong.MinValue;
            combinedBitmask |= entityBitmaskComponent?.Bitmask ?? ulong.MinValue | otherEntityBitmaskComponent?.Bitmask ?? ulong.MinValue;
            
            var combinedBitmaskEffects =
                entityBitmaskComponent?.Bitmasks.Where(x => (x.Key & combinedBitmask) != 0)
                    .Aggregate((ulong)0, (current, bitmask) => current | bitmask.Value) ?? ulong.MinValue |
                otherEntityBitmaskComponent?.Bitmasks.Where(x => (x.Key & combinedBitmask) != 0)
                    .Aggregate((ulong)0, (current, bitmask) => current | bitmask.Value) ?? ulong.MinValue;
            return (combinedBitmaskEffects & (ulong)Bitmask) != 0;
        }
    }
}