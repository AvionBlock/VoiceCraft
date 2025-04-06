using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LiteNetLib;

namespace VoiceCraft.Core
{
    public class VoiceCraftWorld : IDisposable //Make this disposable BECAUSE WHY THE FUCK NOT?!
    {
        private int _idIndex = -1;
        private readonly ConcurrentQueue<int> _recycledIds = new ConcurrentQueue<int>();

        public event Action<VoiceCraftEntity>? OnEntityCreated;
        public event Action<VoiceCraftEntity>? OnEntityDestroyed;

        public Dictionary<int, VoiceCraftEntity> Entities { get; } = new Dictionary<int, VoiceCraftEntity>();

        public VoiceCraftEntity CreateEntity()
        {
            var id = GetNextNegativeId();
            var entity = new VoiceCraftEntity(id);
            if (!Entities.TryAdd(id, entity)) throw new InvalidOperationException($"An entity with the id of {id} already exists!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public VoiceCraftNetworkEntity CreateEntity(NetPeer netPeer)
        {
            var entity = new VoiceCraftNetworkEntity(netPeer);
            if (!Entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException($"An entity with the id of {netPeer.Id} already exists!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
            return entity;
        }

        public void AddEntity(VoiceCraftEntity entity)
        {
            if(!Entities.TryAdd(entity.Id, entity))
                throw new InvalidOperationException($"An entity with the id of {entity.Id} already exists!");
            
            entity.OnDestroyed += DestroyEntity;
            OnEntityCreated?.Invoke(entity);
        }

        public bool DestroyEntity(int id)
        {
            if (!Entities.Remove(id, out var entity)) return false;
            entity.Destroy();
            OnEntityDestroyed?.Invoke(entity);
            _recycledIds.Enqueue(id);
            return true;
        }

        public void Dispose()
        {
            foreach (var entity in Entities)
            {
                entity.Value.OnDestroyed -= DestroyEntity; //Don't trigger the events!
                entity.Value.Destroy();
            }
            
            Entities.Clear();

            //Deregister all events.
            OnEntityDestroyed = null;
            OnEntityDestroyed = null;
        }

        private void DestroyEntity(VoiceCraftEntity entity)
        {
            entity.OnDestroyed -= DestroyEntity;
            DestroyEntity(entity.Id);
        }

        private int GetNextNegativeId()
        {
            if (_recycledIds.TryDequeue(out var id)) return id;
            if (_idIndex <= int.MinValue) throw new InvalidOperationException("Cannot allocate a new entity Id, max negative Id has been reached!");
            return _idIndex--;
        }
    }
}