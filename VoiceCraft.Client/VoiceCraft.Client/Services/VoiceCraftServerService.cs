using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Controls;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Services;

public class VoiceCraftServerService : IDisposable
{
    public bool IsRunning { get; private set; }
    private ServiceProvider ServiceProvider { get; }
    private Server.Runtime.App Server { get; }

    public EventBufferedTextWriter ConsoleEventOutput { get; }

    public event Action? OnStarted;
    public event Action<Exception?>? OnStopped;

    public VoiceCraftServerService()
    {
        ServiceProvider = BuildServiceProvider();
        Server = ServiceProvider.GetRequiredService<Server.Runtime.App>();
        ConsoleEventOutput = new EventBufferedTextWriter();
    }

    public async Task StartAsync(Server.Runtime.RuntimeOptions runtimeOptions)
    {
        try
        {
            IsRunning = true;
            
            runtimeOptions.TextWriter = ConsoleEventOutput;
            ConsoleEventOutput.Clear();
            
            OnStarted?.Invoke();
            await Server.StartAsync(runtimeOptions);
        }
        catch (Exception ex)
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
        finally
        {
            IsRunning = false;
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
        serviceCollection.AddSingleton<Server.Runtime.App>(x => new Server.Runtime.App(x));

        //Servers
        serviceCollection.AddSingleton<Network.Servers.LiteNetVoiceCraftServer>();
        serviceCollection.AddSingleton<Network.Servers.HttpMcApiServer>();
        serviceCollection.AddSingleton<Network.Servers.TcpMcApiServer>();
        serviceCollection.AddSingleton<Network.Servers.McWssMcApiServer>();
        serviceCollection.AddSingleton<Network.Servers.VoiceCraftServer>(x => x.GetRequiredService<Network.Servers.LiteNetVoiceCraftServer>());
        serviceCollection.AddSingleton<Network.Servers.McApiServer>(x => x.GetRequiredService<Network.Servers.HttpMcApiServer>());
        serviceCollection.AddSingleton<Network.Servers.McApiServer>(x => x.GetRequiredService<Network.Servers.TcpMcApiServer>());
        serviceCollection.AddSingleton<Network.Servers.McApiServer>(x => x.GetRequiredService<Network.Servers.McWssMcApiServer>());

        //Systems
        serviceCollection.AddSingleton<Server.Runtime.Systems.EventHandlerSystem>();
        serviceCollection.AddSingleton<Network.Systems.AudioEffectSystem>();
        serviceCollection.AddSingleton<Network.Systems.VisibilitySystem>();

        //Commands
        var rootCommand = new RootCommand();
        serviceCollection.AddSingleton(rootCommand);
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.SetPositionCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.SetWorldIdCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.ListCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.SetTitleCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.SetDescriptionCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.SetNameCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.StopCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.MuteCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.UnmuteCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.DeafenCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.UndeafenCommand>();
        serviceCollection.AddSingleton<Command, Server.Runtime.Commands.KickCommand>();

        //Other
        serviceCollection.AddSingleton<Server.Runtime.ServerProperties>();
        serviceCollection.AddSingleton<Server.Runtime.ServerTelemetryService>();
        serviceCollection.AddSingleton<Server.Runtime.Services.PortMappingService>();
        serviceCollection.AddSingleton<VoiceCraftWorld>();
        return serviceCollection.BuildServiceProvider();
    }
}