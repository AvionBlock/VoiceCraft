using System.Numerics;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Systems
{
    public class EventHandlerSystem : IDisposable
    {
        private readonly VoiceCraftServer _server;
        private readonly VoiceCraftWorld _world;
        private readonly AudioEffectSystem _audioEffectSystem;
        private readonly List<Action> _tasks = [];

        public EventHandlerSystem(VoiceCraftServer server)
        {
            _server = server;
            _world = server.World;
            _audioEffectSystem = server.AudioEffectSystem;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
            _audioEffectSystem.OnEffectSet += OnAudioEffectSet;
            _audioEffectSystem.OnEffectRemoved += OnAudioEffectRemoved;
        }

        public void Update()
        {
            Parallel.ForEach(_tasks, task => task.Invoke());
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
        private void OnAudioEffectSet(byte index, IAudioEffect effect)
        {
            _tasks.Add(() =>
            {
                var packet = new SetEffectPacket(index, effect);
                _server.Broadcast(packet);
            });
        }

        private void OnAudioEffectRemoved(byte index, IAudioEffect effect)
        {
            _tasks.Add(() =>
            {
                var packet = new SetEffectPacket(index);
                _server.Broadcast(packet);
            });
        }
        #endregion

        #region Entity Events
        //World
        private void OnEntityCreated(VoiceCraftEntity newEntity)
        {
            //Broadcast entity creation.
            var networkEntity = newEntity as VoiceCraftNetworkEntity;
            var newEntityCreatedPacket = new EntityCreatedPacket(newEntity);
            _server.Broadcast(newEntityCreatedPacket, DeliveryMethod.ReliableOrdered, networkEntity?.NetPeer);
            
            if (networkEntity != null)
            {
                //Send other entities.
                foreach (var entity in _world.Entities)
                {
                    if(entity == newEntity) continue;
                    var entityCreatedPacket = new EntityCreatedPacket(entity);
                    _server.SendPacket(networkEntity.NetPeer, entityCreatedPacket);
                }

                //Send Effects
                foreach (var effect in _audioEffectSystem.Effects)
                {
                    var packet = new SetEffectPacket(effect.Key, effect.Value);
                    _server.SendPacket(networkEntity.NetPeer, packet);
                }
            }
            
            newEntity.OnNameUpdated += OnEntityNameUpdated;
            newEntity.OnTalkBitmaskUpdated += OnEntityTalkBitmaskUpdated;
            newEntity.OnListenBitmaskUpdated += OnEntityListenBitmaskUpdated;
            newEntity.OnMinRangeUpdated += OnEntityMinRangeUpdated;
            newEntity.OnMaxRangeUpdated += OnEntityMaxRangeUpdated;
            newEntity.OnPositionUpdated += OnEntityPositionUpdated;
            newEntity.OnRotationUpdated += OnEntityRotationUpdated;
            newEntity.OnPropertySet += OnEntityPropertySet;
            newEntity.OnVisibleEntityAdded += OnEntityVisibleEntityAdded;
            newEntity.OnVisibleEntityRemoved += OnEntityVisibleEntityRemoved;
            newEntity.OnAudioReceived += OnEntityAudioReceived;
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            var entityDestroyedPacket = new EntityDestroyedPacket(entity.Id);
            if (entity is VoiceCraftNetworkEntity networkEntity)
            {
                networkEntity.NetPeer.Disconnect(); //Disconnect the entity if it's a network entity.
            }
            _server.Broadcast(entityDestroyedPacket);

            entity.OnNameUpdated -= OnEntityNameUpdated;
            entity.OnTalkBitmaskUpdated -= OnEntityTalkBitmaskUpdated;
            entity.OnListenBitmaskUpdated -= OnEntityListenBitmaskUpdated;
            entity.OnMinRangeUpdated -= OnEntityMinRangeUpdated;
            entity.OnMaxRangeUpdated -= OnEntityMaxRangeUpdated;
            entity.OnPositionUpdated -= OnEntityPositionUpdated;
            entity.OnRotationUpdated -= OnEntityRotationUpdated;
            entity.OnPropertySet -= OnEntityPropertySet;
            entity.OnVisibleEntityAdded -= OnEntityVisibleEntityAdded;
            entity.OnVisibleEntityRemoved -= OnEntityVisibleEntityRemoved;
            entity.OnAudioReceived -= OnEntityAudioReceived;
        }

        //Data
        private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetNamePacket(entity.Id, name);
                _server.Broadcast(packet);
            });
        }

        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetTalkBitmaskPacket(entity.Id, bitmask);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetListenBitmaskPacket(entity.Id, bitmask);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        private void OnEntityMinRangeUpdated(int minRange, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetMinRangePacket(entity.Id, minRange);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }
        
        private void OnEntityMaxRangeUpdated(int maxRange, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetMaxRangePacket(entity.Id, maxRange);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                var packet = new SetPositionPacket(entity.Id, position);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                //Only send updates to visible entities.
                var packet = new SetRotationPacket(entity.Id, rotation);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        //Properties
        private void OnEntityPropertySet(PropertyKey key, object? value, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                //Only send updates to visible entities.
                var packet = new SetPropertyPacket(entity.Id, key, value);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }

        private void OnEntityVisibleEntityAdded(VoiceCraftEntity addedEntity, VoiceCraftEntity entity)
        {
            if (addedEntity is not VoiceCraftNetworkEntity networkEntity) return;
            _tasks.Add(() =>
            {
                var visibilityPacket = new SetVisibilityPacket(entity.Id, true);
                var talkBitmaskPacket = new SetTalkBitmaskPacket(entity.Id, entity.TalkBitmask);
                var listenBitmaskPacket = new SetListenBitmaskPacket(entity.Id, entity.ListenBitmask);
                var maxRangePacket = new SetMaxRangePacket(entity.Id, entity.MaxRange);
                var minRangePacket = new SetMinRangePacket(entity.Id, entity.MinRange);
                var positionPacket = new SetPositionPacket(entity.Id, entity.Position);
                var rotationPacket = new SetRotationPacket(entity.Id, entity.Rotation);
                
                _server.SendPacket(networkEntity.NetPeer, visibilityPacket);
                _server.SendPacket(networkEntity.NetPeer, talkBitmaskPacket);
                _server.SendPacket(networkEntity.NetPeer, listenBitmaskPacket);
                _server.SendPacket(networkEntity.NetPeer, maxRangePacket);
                _server.SendPacket(networkEntity.NetPeer, minRangePacket);
                _server.SendPacket(networkEntity.NetPeer, positionPacket);
                _server.SendPacket(networkEntity.NetPeer, rotationPacket);

                foreach (var property in networkEntity.Properties)
                {
                    var propertyPacket = new SetPropertyPacket(entity.Id, property.Key, property.Value); //Set all properties.
                    _server.SendPacket(networkEntity.NetPeer, propertyPacket);
                }
            });
        }
        
        private void OnEntityVisibleEntityRemoved(VoiceCraftEntity removedEntity, VoiceCraftEntity entity)
        {
            if (removedEntity is not VoiceCraftNetworkEntity networkEntity) return;
            _tasks.Add(() =>
            {
                _server.SendPacket(networkEntity.NetPeer, new SetVisibilityPacket(entity.Id));
            });
        }
        
        private void OnEntityAudioReceived(byte[] data, uint timestamp, float frameLoudness, VoiceCraftEntity entity)
        {
            _tasks.Add(() =>
            {
                //Only send updates to visible entities.
                var packet = new AudioPacket(entity.Id, timestamp, frameLoudness, data.Length, data);
                var visibleNetworkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>().Where(x => x != entity);
                foreach (var visibleEntity in visibleNetworkEntities)
                {
                    _server.SendPacket(visibleEntity.NetPeer, packet);
                }
            });
        }
        #endregion
    }
}