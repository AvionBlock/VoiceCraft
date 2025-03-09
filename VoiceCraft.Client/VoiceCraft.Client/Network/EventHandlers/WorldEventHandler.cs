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
    }
}