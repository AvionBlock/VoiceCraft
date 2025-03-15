using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld
    {
        public event Action<VoiceCraftEntity>? OnEntityAdded;
        public event Action<VoiceCraftEntity>? OnEntityRemoved;
        
        public Dictionary<int, VoiceCraftEntity> Entities { get; } = new Dictionary<int, VoiceCraftEntity>();

        public bool AddEntity(VoiceCraftEntity entity)
        {
            var status = Entities.TryAdd(entity.NetworkId, entity);
            if(status) OnEntityAdded?.Invoke(entity);
            return status;
        }

        public bool RemoveEntity(VoiceCraftEntity entity)
        {
            var status = Entities.Remove(entity.NetworkId);
            if (status) OnEntityRemoved?.Invoke(entity);
            return status;
        }
    }
}