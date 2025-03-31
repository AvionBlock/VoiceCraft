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

        public EntityEventsSystem(VoiceCraftServer server)
        {
            _world = server.World;
            _networkSystem = server.NetworkSystem;
            
            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }
        
        public void Dispose()
        {
            _world.OnEntityCreated -= OnEntityCreated;
            _world.OnEntityDestroyed -= OnEntityDestroyed;
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
            var packet = new SetNamePacket(entity.Id, name);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var packet = new SetTalkBitmaskPacket(entity.Id, bitmask);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }        
        
        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var packet = new SetListenBitmaskPacket(entity.Id, bitmask);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            var packet = new SetPositionPacket(entity.Id, position);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            var packet = new SetRotationPacket(entity.Id, rotation);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        //Audio
        private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
        {
            //Only send updates to visible entities.
            var visibleEntities = entity.VisibleEntities.Where(x => x != entity);
            var packet = new AudioPacket(entity.Id, data, data.Length, timestamp);
            foreach (var visibleEntity in visibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        //Properties
        private void OnEntityIntPropertySet(string key, int value, VoiceCraftEntity entity)
        {
            var packet = new SetIntProperty(entity.Id, key, value);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityBoolPropertySet(string key, bool value, VoiceCraftEntity entity)
        {
            var packet = new SetBoolProperty(entity.Id, key, value);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityFloatPropertySet(string key, float value, VoiceCraftEntity entity)
        {
            var packet = new SetFloatProperty(entity.Id, key, value);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityIntPropertyRemoved(string key, int value, VoiceCraftEntity entity)
        {
            var packet = new RemoveIntProperty(entity.Id, key);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityBoolPropertyRemoved(string key, bool value, VoiceCraftEntity entity)
        {
            var packet = new RemoveBoolProperty(entity.Id, key);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityFloatPropertyRemoved(string key, float value, VoiceCraftEntity entity)
        {
            var packet = new RemoveFloatProperty(entity.Id, key);
            foreach (var visibleEntity in entity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, packet);
            }
        }
    }
}