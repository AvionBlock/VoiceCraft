using Friflo.Engine.ECS;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server.EventHandlers
{
    public class WorldEventHandler
    {
        private readonly VoiceCraftServer _server;

        public WorldEventHandler(VoiceCraftServer server)
        {
            _server = server;
            
            _server.World.OnEntityCreate += OnEntityCreate;
            _server.World.OnEntityDelete += OnEntityDelete;
            _server.World.OnComponentAdded += OnComponentAdded;
            _server.World.OnComponentRemoved += OnComponentRemoved;
        }

        private void OnEntityCreate(EntityCreate entityCreate)
        {
            if (!entityCreate.Entity.TryGetComponent(out NetworkComponent networkComponent)) return;
            var entityCreatedPacket = new EntityCreatedPacket();
            var addComponentPacket = new AddComponentPacket();
            var query = _server.World.Query<NetworkComponent>();

            if (networkComponent.Peer != null)
            {
                //Send all entities the new client if the peer is not null.
                query.ForEachEntity((ref NetworkComponent c1, Entity entity) =>
                {
                    entityCreatedPacket.Id = c1.NetworkId;
                    if (entityCreate.Entity.Equals(entity)) return;
                    _server.SendPacket(networkComponent.Peer, entityCreatedPacket);
                    foreach (var enumType in entity.Components.Select(entityComponent => GetComponentTypeEnum(entityComponent.Type.Type)))
                    {
                        if(enumType == null) continue;
                        addComponentPacket.NetworkId = c1.NetworkId;
                        addComponentPacket.ComponentType = (ComponentEnum)enumType;
                        _server.SendPacket(networkComponent.Peer, addComponentPacket);
                    }
                });
            }

            //Send to all other clients.
            var entityComponentTypes = entityCreate.Entity.Components.Select(entityComponent => GetComponentTypeEnum(entityComponent.Type.Type));
            entityCreatedPacket.Id = networkComponent.NetworkId;
            addComponentPacket.NetworkId = networkComponent.NetworkId;
            query.ForEachEntity((ref NetworkComponent c1, Entity entity) =>
            {
                if (entityCreate.Entity.Equals(entity) || c1.Peer == null) return;
                _server.SendPacket(c1.Peer, entityCreatedPacket);
                foreach (var enumType in entityComponentTypes)
                {
                    if(enumType == null) continue;
                    addComponentPacket.ComponentType = (ComponentEnum)enumType;
                    _server.SendPacket(c1.Peer, addComponentPacket);
                }
            });
        }
        
        private void OnEntityDelete(EntityDelete entityDelete)
        {
            if(!entityDelete.Entity.TryGetComponent(out NetworkComponent networkComponent)) return; //We know it's not visible to other clients.
            IdGenerator.Return(networkComponent.NetworkId); //Free the network ID.
            var query = _server.World.Query<NetworkComponent>();
            
            //Send to all other clients.
            var entityDestroyedPacket = new EntityDestroyedPacket
            {
                Id = networkComponent.NetworkId
            };
            query.ForEachEntity((ref NetworkComponent c1, Entity entity) =>
            {
                if (entityDelete.Entity.Equals(entity) || c1.Peer == null) return;
                _server.SendPacket(c1.Peer, entityDestroyedPacket);
            });
        }
        
        private void OnComponentAdded(ComponentChanged componentChanged)
        {
            if (!componentChanged.Entity.TryGetComponent(out NetworkComponent networkComponent)) return;
            var componentType = GetComponentTypeEnum(componentChanged.Type);
            if(componentType == null) return;
            var addComponentPacket = new AddComponentPacket()
            {
                NetworkId = networkComponent.NetworkId,
                ComponentType = (ComponentEnum)componentType
            };
            var query = _server.World.Query<NetworkComponent>();
            
            query.ForEachEntity((ref NetworkComponent c1, Entity _) =>
            {
                if (c1.Peer == null) return;
                _server.SendPacket(c1.Peer, addComponentPacket);
            });
        }
        
        private void OnComponentRemoved(ComponentChanged componentChanged)
        {
            if (!componentChanged.Entity.TryGetComponent(out NetworkComponent networkComponent)) return;
            var componentType = GetComponentTypeEnum(componentChanged.Type);
            if(componentType == null) return;
            var removeComponentPacket = new RemoveComponentPacket
            {
                NetworkId = networkComponent.NetworkId,
                ComponentType = (ComponentEnum)componentType
            };
            var query = _server.World.Query<NetworkComponent>();
            
            query.ForEachEntity((ref NetworkComponent c1, Entity _) =>
            {
                if (c1.Peer == null) return;
                _server.SendPacket(c1.Peer, removeComponentPacket);
            });
        }
        
        private static ComponentEnum? GetComponentTypeEnum(Type type)
        {
            //You can't compare types in a switch.
            return type.Name switch
            {
                nameof(TransformComponent) => ComponentEnum.TransformComponent,
                _ => null
            };
        }
    }
}