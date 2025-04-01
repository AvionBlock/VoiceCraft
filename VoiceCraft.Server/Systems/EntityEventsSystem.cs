using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class EntityEventsSystem : IDisposable
    {
        private readonly VoiceCraftWorld _world;
        private readonly NetworkSystem _networkSystem;
        private readonly List<Task> _tasks = [];

        public EntityEventsSystem(VoiceCraftServer server)
        {
            _world = server.World;
            _networkSystem = server.NetworkSystem;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }

        public void Update()
        {
            foreach (var task in _tasks)
            {
                task.Start();
            }

            var t = Task.WhenAll(_tasks);
            t.Wait();
            _tasks.Clear();
        }

        public void Dispose()
        {
            _world.OnEntityCreated -= OnEntityCreated;
            _world.OnEntityDestroyed -= OnEntityDestroyed;
            GC.SuppressFinalize(this);
        }

        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            entity.OnNameUpdated += OnEntityNameUpdated;
            entity.OnTalkBitmaskUpdated += OnEntityTalkBitmaskUpdated;
            entity.OnListenBitmaskUpdated += OnEntityListenBitmaskUpdated;
            entity.OnPositionUpdated += OnEntityPositionUpdated;
            entity.OnRotationUpdated += OnEntityRotationUpdated;
            entity.OnAudioReceived += OnEntityAudioReceived;
            entity.OnIntPropertySet += OnEntityIntPropertySet;
            entity.OnBoolPropertySet += OnEntityBoolPropertySet;
            entity.OnFloatPropertySet += OnEntityFloatPropertySet;
            entity.OnIntPropertyRemoved += OnEntityIntPropertyRemoved;
            entity.OnBoolPropertyRemoved += OnEntityBoolPropertyRemoved;
            entity.OnFloatPropertyRemoved += OnEntityFloatPropertyRemoved;
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            entity.OnNameUpdated -= OnEntityNameUpdated;
            entity.OnTalkBitmaskUpdated -= OnEntityTalkBitmaskUpdated;
            entity.OnListenBitmaskUpdated -= OnEntityListenBitmaskUpdated;
            entity.OnPositionUpdated -= OnEntityPositionUpdated;
            entity.OnRotationUpdated -= OnEntityRotationUpdated;
            entity.OnIntPropertySet -= OnEntityIntPropertySet;
            entity.OnBoolPropertySet -= OnEntityBoolPropertySet;
            entity.OnFloatPropertySet -= OnEntityFloatPropertySet;
            entity.OnIntPropertyRemoved -= OnEntityIntPropertyRemoved;
            entity.OnBoolPropertyRemoved -= OnEntityBoolPropertyRemoved;
            entity.OnFloatPropertyRemoved -= OnEntityFloatPropertyRemoved;
        }

        //Data
        private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetNamePacket(entity.Id, name);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetTalkBitmaskPacket(entity.Id, bitmask);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetListenBitmaskPacket(entity.Id, bitmask);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetPositionPacket(entity.Id, position);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetRotationPacket(entity.Id, rotation);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        //Audio
        private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                //Only send updates to visible entities.
                var visibleEntities = entity.VisibleEntities.Where(x => x != entity);
                var packet = new AudioPacket(entity.Id, timestamp, (ushort)data.Length, data);
                foreach (var visibleEntity in visibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        //Properties
        private void OnEntityIntPropertySet(string key, int value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetIntProperty(entity.Id, key, value);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityBoolPropertySet(string key, bool value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetBoolProperty(entity.Id, key, value);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityFloatPropertySet(string key, float value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetFloatProperty(entity.Id, key, value);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityIntPropertyRemoved(string key, int value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new RemoveIntProperty(entity.Id, key);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityBoolPropertyRemoved(string key, bool value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new RemoveBoolProperty(entity.Id, key);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }

        private void OnEntityFloatPropertyRemoved(string key, float value, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new RemoveFloatProperty(entity.Id, key);
                foreach (var visibleEntity in entity.VisibleEntities)
                {
                    _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
                }
            }));
        }
    }
}