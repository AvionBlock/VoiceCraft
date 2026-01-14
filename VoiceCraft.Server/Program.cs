using System.CommandLine;
using System.Diagnostics;
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

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Server.Locales");
        FleckLog.LogAction = (_, _, _) => { }; //Remove all websocket logs.
        LogService.Load(); //Load Logs.
        new VoiceCraftRootCommand().Parse(args).InvokeAsync().GetAwaiter().GetResult();
        ServiceProvider.Dispose(); //Dispose
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        //Servers
        serviceCollection.AddSingleton<VoiceCraftServer>();
        serviceCollection.AddSingleton<McWssServer>();
        serviceCollection.AddSingleton<McHttpServer>();

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
        serviceCollection.AddSingleton<VoiceCraftWorld>();
        return serviceCollection.BuildServiceProvider();
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                LogService.LogCrash(ex); //Log it
        }
        catch (Exception writeEx)
        {
            Debug.WriteLine(writeEx); //We don't want to crash if the log failed.
        }
    }
}