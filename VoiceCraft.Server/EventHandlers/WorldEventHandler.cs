using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class WorldEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly VoiceCraftWorld _world;

        public WorldEventHandler(VoiceCraftServer server)
        {
            _server = server;
            _world = _server.World;

            _world.OnEntityAdded += OnEntityAdded;
            _world.OnEntityRemoved += OnEntityRemoved;
        }

        private void OnEntityAdded(VoiceCraftEntity newEntity)
        {
            var entityCreatedPacket = new EntityCreatedPacket()
            {
                NetworkId = newEntity.NetworkId,
                Bitmask = newEntity.Bitmask,
                Name = newEntity.Name
            };

            var setEffectPacket = new SetEffectPacket()
            {
                NetworkId = newEntity.NetworkId
            };

            foreach (var entity in _world.Entities.Values.OfType<VoiceCraftNetworkEntity>())
            {
                if (entity == newEntity) continue;
                //Send newly created entity to all other entities.
                _server.SendPacket(entity.NetPeer, entityCreatedPacket);
                foreach (var effect in newEntity.Effects.Values)
                {
                    //Send all the effects of the entity.
                    setEffectPacket.Effect = effect;
                    _server.SendPacket(entity.NetPeer, setEffectPacket);
                }
            }
            
            if (newEntity is not VoiceCraftNetworkEntity networkEntity) return;
            foreach (var entity in _world.Entities.Values)
            {
                if (entity == newEntity) continue;
                //Set new packet data.
                entityCreatedPacket.Name = entity.Name;
                entityCreatedPacket.Bitmask = entity.Bitmask;
                entityCreatedPacket.NetworkId = entity.NetworkId;
                setEffectPacket.NetworkId = entity.NetworkId;
                //Send entity to the new entity.
                _server.SendPacket(networkEntity.NetPeer, entityCreatedPacket);
                //Send all the effects of the entity.
                foreach (var effect in entity.Effects.Values)
                {
                    setEffectPacket.Effect = effect;
                    _server.SendPacket(networkEntity.NetPeer, setEffectPacket);
                }
            }
        }

        private void OnEntityRemoved(VoiceCraftEntity removedEntity)
        {
            if (removedEntity is VoiceCraftNetworkEntity networkentity)
            {
                networkentity.NetPeer.Disconnect(); //Disconnect if it's a client entity.
            }

            var entityCreatedPacket = new EntityRemovedPacket()
            {
                NetworkId = removedEntity.NetworkId
            };

            foreach (var entity in _world.Entities.Values.OfType<VoiceCraftNetworkEntity>())
            {
                _server.SendPacket(entity.NetPeer, entityCreatedPacket);
            }
        }
    }
}