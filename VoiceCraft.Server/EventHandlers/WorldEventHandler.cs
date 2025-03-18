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

        private static void OnEntityAdded(VoiceCraftEntity newEntity)
        {
        }

        private static void OnEntityRemoved(VoiceCraftEntity removedEntity)
        {
            if (removedEntity is VoiceCraftNetworkEntity networkentity)
            {
                networkentity.NetPeer.Disconnect(); //Disconnect if it's a client entity.
            }
        }
    }
}