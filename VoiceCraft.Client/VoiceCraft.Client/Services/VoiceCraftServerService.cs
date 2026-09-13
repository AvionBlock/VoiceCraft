using System.Threading.Tasks;
using VoiceCraft.Server;

namespace VoiceCraft.Client.Services;

public class VoiceCraftServerService
{
    public async Task RunAsync(RuntimeOptions runtimeOptions)
    {
        await VoiceCraft.Server.App.StartAsync(runtimeOptions);
    }
}