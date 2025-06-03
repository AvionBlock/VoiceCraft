using System.Linq;
using System.Threading.Tasks;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network.Systems;

public class EntityTickSystem(VoiceCraftClient client)
{
    private readonly VoiceCraftClient _client = client;
    private readonly VoiceCraftWorld _world = client.World;

    public void TickEntities()
    {
        Parallel.ForEach(_world.Entities.OfType<VoiceCraftClientEntity>(), entity => entity.Tick());
    }
}