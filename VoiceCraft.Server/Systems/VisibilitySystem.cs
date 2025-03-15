using Arch.Core;
using Arch.System;

namespace VoiceCraft.Server.Systems
{
    public class VisibilitySystem : BaseSystem<World, float>
    {
        private readonly VoiceCraftServer _server;
        
        public VisibilitySystem(World world) : base(world)
        {
        }
    }
}