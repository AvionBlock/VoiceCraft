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
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new SetNamePacket(entity.Id, name);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new SetTalkBitmaskPacket(entity.Id, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }        
        
        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new SetListenBitmaskPacket(entity.Id, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new SetPositionPacket(entity.Id, position);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new SetRotationPacket(entity.Id, rotation);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        //Audio
        private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
        {
            //Only send updates to visible entities.
            var networkEntities = entity.VisibleEntities.Where(x => x != entity);
            var packet = new AudioPacket(entity.Id, data, data.Length, timestamp);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        //Properties
        private void OnEntityIntPropertySet(string arg1, int arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
        
        private void OnEntityBoolPropertySet(string arg1, bool arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
        
        private void OnEntityFloatPropertySet(string arg1, float arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
        
        private void OnEntityIntPropertyRemoved(string arg1, int arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
        
        private void OnEntityBoolPropertyRemoved(string arg1, bool arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
        
        private void OnEntityFloatPropertyRemoved(string arg1, float arg2, VoiceCraftEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}