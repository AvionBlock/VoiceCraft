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
            entity.OnAudioReceived += OnEntityAudioReceived;
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
        
        //Data
        private void OnEntityNameUpdated(string name, VoiceCraftEntity entity)
        {
            var packet = new UpdateNamePacket(entity.Id, name);
            _networkSystem.Broadcast(packet);
        }
        
        private void OnEntityTalkBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.Values.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateTalkBitmaskPacket(entity.Id, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }        
        
        private void OnEntityListenBitmaskUpdated(ulong bitmask, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.Values.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateListenBitmaskPacket(entity.Id, bitmask);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityPositionUpdated(Vector3 position, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.Values.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdatePositionPacket(entity.Id, position);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityRotationUpdated(Quaternion rotation, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.Values.OfType<VoiceCraftNetworkEntity>();
            var packet = new UpdateRotationPacket(entity.Id, rotation);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }

        //Effects
        private void OnEntityEffectAdded(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var packet = new AddEffectPacket(entity.Id, effect.EffectType);
            _networkSystem.Broadcast(packet);
        }
        
        private void OnEntityEffectUpdated(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var networkEntities = entity.VisibleEntities.Values.OfType<VoiceCraftNetworkEntity>(); //Only send updates to visible entities.
            var packet = new UpdateEffectPacket(entity.Id, effect);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
        
        private void OnEntityEffectRemoved(IAudioEffect effect, VoiceCraftEntity entity)
        {
            var packet = new RemoveEffectPacket(entity.Id, effect.EffectType);
            _networkSystem.Broadcast(packet);
        }
        
        //Audio
        private void OnEntityAudioReceived(byte[] data, uint timestamp, VoiceCraftEntity entity)
        {
            //Only send updates to visible entities.
            var networkEntities = entity.VisibleEntities.Values.Where(x => x != entity).OfType<VoiceCraftNetworkEntity>();
            var packet = new AudioPacket(entity.Id, data, data.Length, timestamp);
            foreach (var networkEntity in networkEntities)
            {
                _networkSystem.SendPacket(networkEntity.NetPeer, packet);
            }
        }
    }
}