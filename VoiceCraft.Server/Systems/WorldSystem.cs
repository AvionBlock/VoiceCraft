using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Systems
{
    public class WorldSystem : IDisposable
    {
        private readonly VoiceCraftWorld _world;
        private readonly NetworkSystem _networkSystem;
        private readonly AudioEffectSystem _audioEffectSystem;

        public WorldSystem(VoiceCraftServer server)
        {
            _world = server.World;
            _networkSystem = server.NetworkSystem;
            _audioEffectSystem = server.AudioEffectSystem;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }
        
        public void Dispose()
        {
            _world.OnEntityCreated -= OnEntityCreated;
            _world.OnEntityDestroyed -= OnEntityDestroyed;
            GC.SuppressFinalize(this);
        }

        private void OnEntityCreated(VoiceCraftEntity newEntity)
        {
            //Visibility system will handle entity creation and deletion for all entities.
            if (newEntity is not VoiceCraftNetworkEntity newNetworkEntity) return;
            var effectsCount = _audioEffectSystem.Effects.Count();
            for (byte i = 0; i < effectsCount; i++)
            {
                var effect = _audioEffectSystem.Effects.ElementAt(i);
                if(effect == null) continue;
                var setEffectPacket = new SetEffectPacket(i, effect);
                _networkSystem.SendPacket(newNetworkEntity.NetPeer, setEffectPacket);
            }
        }

        private void OnEntityDestroyed(VoiceCraftEntity removedEntity)
        {
            if (removedEntity is VoiceCraftNetworkEntity removedNetworkentity)
            {
                removedNetworkentity.NetPeer.Disconnect(); //Disconnect if it's a client entity.
            }

            var entityDestroyedPacket = new EntityDestroyedPacket(removedEntity.Id);
            foreach (var visibleEntity in removedEntity.VisibleEntities)
            {
                _networkSystem.SendPacket(visibleEntity.NetPeer, entityDestroyedPacket);
            }
        }
    }
}