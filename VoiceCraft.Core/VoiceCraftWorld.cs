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

        public bool CreateEntity()
        {
            var id = GetNextNegativeId();
            var entity = new VoiceCraftEntity(id);
            if (!Entities.TryAdd(id, entity)) return false;
            OnEntityCreated?.Invoke(entity);
            return true;
        }

        public bool CreateEntity(NetPeer netPeer)
        {
            var entity = new VoiceCraftEntity(netPeer.Id);
            if (!Entities.TryAdd(netPeer.Id, new VoiceCraftEntity(netPeer.Id))) return false;
            OnEntityCreated?.Invoke(entity);
            return true;
        }

        public bool DestroyEntity(int id)
        {
            if (!Entities.TryRemove(id, out _)) return false;
            OnEntityDestroyed?.Invoke(Entities[id]);
            _recycledIds.Enqueue(id);
            return true;
        }

        private int GetNextNegativeId()
        {
            if (_recycledIds.TryDequeue(out var id)) return id;
            if (_idIndex <= int.MinValue) throw new InvalidOperationException("Cannot allocate a new entity Id, max negative Id has been reached!");
            return _idIndex--;
        }
    }
}