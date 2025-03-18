using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld
    {
        public event Action<VoiceCraftEntity>? OnEntityAdded;
        public event Action<VoiceCraftEntity>? OnEntityRemoved;

        public List<VoiceCraftEntity> Entities { get; } = new List<VoiceCraftEntity>();

        public void AddEntity(VoiceCraftEntity entity)
        {
            Entities.Add(entity);
            OnEntityAdded?.Invoke(entity);
        }

        public bool RemoveEntity(VoiceCraftEntity entity)
        {
            if(!Entities.Remove(entity)) return false;
            OnEntityRemoved?.Invoke(entity);
            return true;
        }
    }
}