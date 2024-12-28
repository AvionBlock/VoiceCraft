using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class AudioBitmaskComponent : IComponent<AudioBitmaskComponent>
    {
        private readonly Dictionary<ulong, ulong> _bitmasks = new Dictionary<ulong, ulong>();
        private ulong _bitmask;
        public event Action<AudioBitmaskComponent>? OnUpdate;
        public event Action<AudioBitmaskComponent>? OnDestroy;
        public Guid Id { get; } = Guid.NewGuid();
        public World World { get; }
        public Entity Entity { get; }

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value) return;
                _bitmask = value;
                OnUpdate?.Invoke(this);
            }
        }

        public AudioBitmaskComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }

        public void AddBitmask(ulong bitmask, ulong effectBitmask)
        {
            var existingBitmasks = _bitmasks.Where(existingBitmask => (existingBitmask.Key & bitmask) != 0 || existingBitmask.Key == bitmask).ToArray();
            if (existingBitmasks.Length != 0)
                throw new InvalidOperationException($"Bitmask conflicts with another existing bitmask of value {existingBitmasks[0].Key}.");

            _bitmasks.Add(bitmask, effectBitmask);
        }

        public bool UpdateBitmask(ulong bitmask, ulong effectBitmask)
        {
            if (!_bitmasks.ContainsKey(bitmask)) return false;
            _bitmasks[bitmask] = effectBitmask;
            return true;
        }

        public bool RemoveBitmask(ulong bitmask)
        {
            return _bitmasks.Remove(bitmask);
        }

        public ulong GetEnabledBitmaskEffects(ulong comparisonBitmask)
        {
            return _bitmasks.Where(bitmask => (bitmask.Key & comparisonBitmask) != 0)
                .Aggregate((ulong)0, (current, bitmask) => current | bitmask.Value);
        }
        
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}