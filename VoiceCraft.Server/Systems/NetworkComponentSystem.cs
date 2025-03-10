using Arch.Core;
using Arch.System;
using VoiceCraft.Core.Components;

namespace VoiceCraft.Server.Systems
{
    public partial class NetworkComponentSystem : BaseSystem<World, float>
    {
        public NetworkComponentSystem(World world) : base(world) {}

        [Query]
        [All(typeof(NetworkComponent))]
        public void CalculateVisibleEntities([Data] in float deltaTime, ref Entity entity, ref NetworkComponent networkComponent)
        {
            Console.WriteLine(entity);
        }
    }
}