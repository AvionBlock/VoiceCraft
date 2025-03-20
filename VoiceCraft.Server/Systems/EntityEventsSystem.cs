using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class EntityEventsSystem
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
        
        private void OnEntityCreated(VoiceCraftEntity entity)
        {
            entity.OnNameUpdated += OnEntityNameUpdated;
            entity.OnTalkBitmaskUpdated += OnEntityTalkBitmaskUpdated;
            entity.OnListenBitmaskUpdated += OnEntityListenBitmaskUpdated;
            entity.OnPositionUpdated += OnEntityPositionUpdated;
            entity.OnRotationUpdated += OnEntityRotationUpdated;
            entity.OnEffectAdded += OnEntityEffectAdded;
            entity.OnEffectUpdated += OnEntityEffectUpdated;
            entity.OnEffectRemoved += OnEntityEffectRemoved;
        }

        private void OnEntityDestroyed(VoiceCraftEntity entity)
        {
            entity.OnNameUpdated -= OnEntityNameUpdated;
            entity.OnTalkBitmaskUpdated -= OnEntityTalkBitmaskUpdated;
            entity.OnListenBitmaskUpdated -= OnEntityListenBitmaskUpdated;
            entity.OnPositionUpdated -= OnEntityPositionUpdated;
            entity.OnRotationUpdated -= OnEntityRotationUpdated;
            entity.OnEffectAdded -= OnEntityEffectAdded;
            entity.OnEffectUpdated -= OnEntityEffectUpdated;
            entity.OnEffectRemoved -= OnEntityEffectRemoved;
        }
        
        private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateNamePacket(entity.NetworkId, name);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateTalkBitmaskPacket(entity.NetworkId, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }        
        
        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateListenBitmaskPacket(entity.NetworkId, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdatePositionPacket(entity.NetworkId, position);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateRotationPacket(entity.NetworkId, rotation);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }

        private void OnEntityEffectAdded(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new AddEffectPacket(entity.NetworkId, effect);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityEffectUpdated(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateEffectPacket(entity.NetworkId, effect);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityEffectRemoved(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.OfType<VoiceCraftNetworkEntity>();
            var packet = new RemoveEffectPacket(entity.NetworkId, effect.EffectType);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
    }
}