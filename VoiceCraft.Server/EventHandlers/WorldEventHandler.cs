using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class WorldEventHandler
    {
        private readonly VoiceCraftServer _server;
        private readonly VoiceCraftWorld _world;
        private readonly List<Task> _tasks = [];

        public WorldEventHandler(VoiceCraftServer server)
        {
            _server = server;
            _world = _server.World;

            _world.OnEntityCreated += OnEntityCreated;
            _world.OnEntityDestroyed += OnEntityDestroyed;
        }

        public void Update()
        {
            var t = Task.WhenAll(_tasks);
            t.Wait();
        }

        private void OnEntityCreated(VoiceCraftEntity newEntity)
        {
            var createEntityPacket = new EntityCreatedPacket(newEntity.NetworkId, newEntity);
            _tasks.Add(Task.Run(() =>
            {
                foreach (var entity in _world.Entities)
                {
                    if (entity.Key == newEntity.NetworkId || entity.Value is not VoiceCraftNetworkEntity networkEntity) continue;
                    _server.SendPacket(networkEntity.NetPeer, createEntityPacket);
                    SendEntityEffects(entity.Value, networkEntity);

                    if (!newEntity.VisibleTo(entity.Value)) continue;
                    SendEntityData(newEntity, networkEntity);
                }
            }));

            if (newEntity is not VoiceCraftNetworkEntity newNetworkEntity) return;
            _tasks.Add(Task.Run(() =>
            {
                foreach (var entity in _world.Entities)
                {
                    if (entity.Key == newNetworkEntity.NetworkId) continue;
                    createEntityPacket = new EntityCreatedPacket(entity.Value.NetworkId, entity.Value);
                    _server.SendPacket(newNetworkEntity.NetPeer, createEntityPacket);
                    SendEntityEffects(entity.Value, newNetworkEntity);

                    if (!entity.Value.VisibleTo(newNetworkEntity)) continue;
                    SendEntityData(entity.Value, newNetworkEntity);
                }
            }));
        }

        private void SendEntityEffects(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            foreach (var effect in entity.Effects)
            {
                var setEffectPacket = new SetEffectPacket(entity.NetworkId, effect.Value);
                _server.SendPacket(targetEntity.NetPeer, setEffectPacket);
            }
        }

        private void SendEntityData(VoiceCraftEntity entity, VoiceCraftNetworkEntity targetEntity)
        {
            var updatePositionPacket = new UpdatePositionPacket(entity.NetworkId, entity.Position);
            var updateRotationPacket = new UpdateRotationPacket(entity.NetworkId, entity.Rotation);

            _server.SendPacket(targetEntity.NetPeer, updatePositionPacket);
            _server.SendPacket(targetEntity.NetPeer, updateRotationPacket);
        }

        private void OnEntityDestroyed(VoiceCraftEntity removedEntity)
        {
            _tasks.Add(Task.Run(() =>
            {
                var destroyEntityPacket = new EntityDestroyedPacket(removedEntity.NetworkId);
                if (removedEntity is VoiceCraftNetworkEntity removedNetworkentity)
                {
                    removedNetworkentity.NetPeer.Disconnect(); //Disconnect if it's a client entity.
                }

                foreach (var entity in _world.Entities)
                {
                    if (entity.Value is not VoiceCraftNetworkEntity networkentity) continue;
                    _server.SendPacket(networkentity.NetPeer, destroyEntityPacket);
                }
            }));
        }
    }
}