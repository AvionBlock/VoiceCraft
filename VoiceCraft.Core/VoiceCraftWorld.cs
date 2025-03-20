using System;
using System.Collections.Concurrent;
using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld
    {
        private int _idIndex = -1;
        private readonly ConcurrentQueue<int> _recycledIds = new ConcurrentQueue<int>();

        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;

        public ConcurrentDictionary<int, VoiceCraftEntity> Entities { get; } = new ConcurrentDictionary<int, VoiceCraftEntity>();

        public VoiceCraftEntity CreateEntity()
        {
            var id = GetNextNegativeId();
            var entity = new VoiceCraftEntity(id);
            if (!Entities.TryAdd(id, entity)) throw new InvalidOperationException($"An entity with the id of {id} already exists!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public VoiceCraftEntity CreateEntity(NetPeer netPeer)
        {
            var entity = new VoiceCraftEntity(netPeer.Id);
            if (!Entities.TryAdd(netPeer.Id, new VoiceCraftEntity(netPeer.Id)))
                throw new InvalidOperationException($"An entity with the id of {netPeer.Id} already exists!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public bool DestroyEntity(int id)
        {
            if (!Entities.TryRemove(id, out var entity)) return false;
            entity.Destroy();
            OnEntityDestroyed?.Invoke(Entities[id]);
            _recycledIds.Enqueue(id);
            return true;
        }

        private void DestroyEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= DestroyEntity;
            DestroyEntity(entity.NetworkId);
        }

        private int GetNextNegativeId()
        {
            if (_recycledIds.TryDequeue(out var id)) return id;
            if (_idIndex <= int.MinValue) throw new InvalidOperationException("Cannot allocate a new entity Id, max negative Id has been reached!");
            return _idIndex--;
        }
    }
}