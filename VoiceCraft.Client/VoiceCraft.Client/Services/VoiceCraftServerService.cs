using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Server;
using VoiceCraft.Server.Commands;
using VoiceCraft.Server.Services;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Client.Services;

public class VoiceCraftServerService : IDisposable
{
    private ServiceProvider ServiceProvider { get; }
    private VoiceCraft.Server.App Server { get; }

    public event Action? OnStarted;
    public event Action<Exception?>? OnStopped;

    public VoiceCraftServerService()
    {
        ServiceProvider = BuildServiceProvider();
        Server = ServiceProvider.GetRequiredService<VoiceCraft.Server.App>();
    }

    public async Task StartAsync(RuntimeOptions runtimeOptions)
    {
        try
        {
            OnStarted?.Invoke();
            await Server.StartAsync(runtimeOptions);
        }
        catch(Exception ex)
        {
            await StopAsync(ex);
            return;
        }
        
        await StopAsync();
    }

    public async Task StopAsync(Exception? ex = null)
    {
        try
        {
            await Server.ShutdownAsync();
        }
        catch
        {
            //Do Nothing
        }
        finally
        {
            OnStopped?.Invoke(ex);
        }
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        //Application
        serviceCollection.AddSingleton<VoiceCraft.Server.App>(x => new VoiceCraft.Server.App(x));

        //Servers
        serviceCollection.AddSingleton<LiteNetVoiceCraftServer>();
        serviceCollection.AddSingleton<HttpMcApiServer>();
        serviceCollection.AddSingleton<TcpMcApiServer>();
        serviceCollection.AddSingleton<McWssMcApiServer>();
        serviceCollection.AddSingleton<VoiceCraftServer>(x => x.GetRequiredService<LiteNetVoiceCraftServer>());
        serviceCollection.AddSingleton<McApiServer>(x => x.GetRequiredService<HttpMcApiServer>());
        serviceCollection.AddSingleton<McApiServer>(x => x.GetRequiredService<TcpMcApiServer>());
        serviceCollection.AddSingleton<McApiServer>(x => x.GetRequiredService<McWssMcApiServer>());

        //Systems
        serviceCollection.AddSingleton<EventHandlerSystem>();
        serviceCollection.AddSingleton<AudioEffectSystem>();
        serviceCollection.AddSingleton<VisibilitySystem>();

        //Commands
        var rootCommand = new RootCommand();
        serviceCollection.AddSingleton(rootCommand);
        serviceCollection.AddSingleton<Command, SetPositionCommand>();
        serviceCollection.AddSingleton<Command, SetWorldIdCommand>();
        serviceCollection.AddSingleton<Command, ListCommand>();
        serviceCollection.AddSingleton<Command, SetTitleCommand>();
        serviceCollection.AddSingleton<Command, SetDescriptionCommand>();
        serviceCollection.AddSingleton<Command, SetNameCommand>();
        serviceCollection.AddSingleton<Command, StopCommand>();
        serviceCollection.AddSingleton<Command, MuteCommand>();
        serviceCollection.AddSingleton<Command, UnmuteCommand>();
        serviceCollection.AddSingleton<Command, DeafenCommand>();
        serviceCollection.AddSingleton<Command, UndeafenCommand>();
        serviceCollection.AddSingleton<Command, KickCommand>();

        //Other
        serviceCollection.AddSingleton<ServerProperties>();
        serviceCollection.AddSingleton<ServerTelemetryService>();
        serviceCollection.AddSingleton<PortMappingService>();
        serviceCollection.AddSingleton<VoiceCraftWorld>();
        return serviceCollection.BuildServiceProvider();
    }
}