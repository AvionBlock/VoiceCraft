using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Components
{
    public class AudioBitmaskComponent : IComponent
    {
        private readonly Dictionary<ulong, ulong> _bitmasks = new Dictionary<ulong, ulong>();
        private ulong _bitmask;
        
        public event Action<IComponent>? OnUpdate;
        public event Action<IComponent>? OnDestroy;
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
        
        public IEnumerable<KeyValuePair<ulong, ulong>> Bitmasks => _bitmasks;

        public AudioBitmaskComponent(World world, Entity entity)
        {
            World = world;
            Entity = entity;
        }
        
        public bool IsVisibleToEntity(Entity otherEntity) => true; //Don't care

        public void AddBitmask(ulong bitmask, ulong effectBitmask)
        {
            var existingBitmasks = _bitmasks.Where(existingBitmask => (existingBitmask.Key & bitmask) != 0 || existingBitmask.Key == bitmask).ToArray();
            if (existingBitmasks.Length != 0)
                throw new InvalidOperationException($"Bitmask conflicts with another existing bitmask of value {existingBitmasks[0].Key}.");

            _bitmasks.Add(bitmask, effectBitmask);
            OnUpdate?.Invoke(this);
        }

        public bool UpdateBitmask(ulong bitmask, ulong effectBitmask)
        {
            if (!_bitmasks.ContainsKey(bitmask)) return false;
            _bitmasks[bitmask] = effectBitmask;
            OnUpdate?.Invoke(this);
            return true;
        }

        public bool RemoveBitmask(ulong bitmask)
        {
            if (!_bitmasks.Remove(bitmask)) return false;
            OnUpdate?.Invoke(this);
            return true;
        }
        
        public void Destroy()
        {
            OnUpdate = null;
            OnDestroy?.Invoke(this);
            OnDestroy = null;
        }
    }
}