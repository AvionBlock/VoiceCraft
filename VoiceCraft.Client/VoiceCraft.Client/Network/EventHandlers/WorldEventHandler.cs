using System;
using VoiceCraft.Core.Components;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Network.EventHandlers
{
    public class WorldEventHandler
    {
        private readonly VoiceCraftClient _client;

        public WorldEventHandler(VoiceCraftClient client)
        {
            _client = client;
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