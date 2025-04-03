using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class EventHandlerSystem : IDisposable
    {
        private readonly VoiceCraftWorld _world;
        private readonly NetworkSystem _networkSystem;
        private readonly AudioEffectSystem _audioEffectSystem;
        private readonly List<Task> _tasks = [];

        public EventHandlerSystem(VoiceCraftServer server)
        {
            _world = server.World;
            _networkSystem = server.NetworkSystem;
            _audioEffectSystem = server.AudioEffectSystem;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
            _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
            _audioEffectSystem.OnEffectRemoved += OnAudioEffectRemoved;
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
            _audioEffectSystem.OnEffectSet -= OnAudioEffectSet;
            _audioEffectSystem.OnEffectRemoved -= OnAudioEffectRemoved;
            GC.SuppressFinalize(this);
        }
        
        #region Audio Effect Events
        private void OnAudioEffectSet(IAudioEffect effect, byte index)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new SetEffectPacket(index, effect);
                _networkSystem.Broadcast(packet);
            }));
        }

        private void OnAudioEffectRemoved(IAudioEffect effect, byte index)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new RemoveEffectPacket(index);
                _networkSystem.Broadcast(packet);
            }));
        }
        #endregion

        #region Entity Events

        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            entity.OnVisibleEntityAdded += OnEntityVisibleEntityAdded;
            entity.OnVisibleEntityRemoved += OnEntityVisibleEntityRemoved;
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
            entity.OnVisibleEntityAdded -= OnEntityVisibleEntityAdded;
            entity.OnVisibleEntityRemoved -= OnEntityVisibleEntityRemoved;
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

        //Visibility
        private void OnEntityVisibleEntityAdded(VoiceCraftNetworkEntity visibleEntity, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var packet = new EntityCreatedPacket(entity.Id, entity);
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }));
        }

        private void OnEntityVisibleEntityRemoved(VoiceCraftNetworkEntity visibleEntity, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                var entityDestroyedPacket = new EntityDestroyedPacket(entity.Id);
                _networkSystem.SendPacket(visibleEntity.NetPeer, entityDestroyedPacket);
            }));
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
        private void OnEntityAudioReceived(byte[] data, uint timestamp, bool endOfTransmission, VoiceCraftEntity entity)
        {
            _tasks.Add(new Task(() =>
            {
                //Only send updates to visible entities.
                var visibleEntities = entity.VisibleEntities.Where(x => x != entity);
                foreach (var visibleEntity in visibleEntities)
                {
                    var packet = new AudioPacket(entity.Id, timestamp, endOfTransmission, data.Length, data);
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

        #endregion
    }
}