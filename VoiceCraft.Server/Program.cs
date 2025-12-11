using System.CommandLine;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Commands;
using VoiceCraft.Server.Locales;
using VoiceCraft.Server.Servers;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server;

public static class Program
{
    public static readonly ServiceProvider ServiceProvider = BuildServiceProvider();

    public static void Main()
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Server.Locales");
        FleckLog.LogAction = (_, _, _) => { }; //Remove all websocket logs.
        App.Start().GetAwaiter().GetResult();
        ServiceProvider.Dispose(); //Dispose
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        //Servers
        serviceCollection.AddSingleton<VoiceCraftServer>();
        serviceCollection.AddSingleton<McWssServer>();
        //serviceCollection.AddSingleton<McHttpServer>();
        
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
        
        //Other
        serviceCollection.AddSingleton<ServerProperties>();
        serviceCollection.AddSingleton<VoiceCraftWorld>();
        return serviceCollection.BuildServiceProvider();
    }
}