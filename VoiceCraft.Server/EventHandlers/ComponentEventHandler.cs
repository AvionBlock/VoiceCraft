using Arch.Core;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Events;

namespace VoiceCraft.Server.EventHandlers
{
    public class ComponentEventHandler(VoiceCraftServer server) : VoiceCraft.Core.Events.ComponentEventHandler
    {
        private readonly VoiceCraftServer _server = server;
        private readonly WorldHandler _world = server.World;

        public override void ComponentUpdated(ref ComponentUpdatedEvent @event)
        {
            var query = new QueryDescription()
                .WithAll<NetworkComponent>();
            _world.Query(in query, entity =>
            {
                var networkComponent = _world.Get<NetworkComponent>(entity);
                //Find all 
            });
            base.ComponentUpdated(ref @event);
        }
    }
}