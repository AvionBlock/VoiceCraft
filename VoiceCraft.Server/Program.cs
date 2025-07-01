using System.CommandLine;
using Jeek.Avalonia.Localization;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Commands;
using VoiceCraft.Server.Locales;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server;

public static class Program
{
    public static readonly IServiceProvider ServiceProvider = BuildServiceProvider();

    public static void Main()
    {
        Localizer.SetLocalizer(new EmbeddedJsonLocalizer("VoiceCraft.Server.Locales"));
        App.Start().GetAwaiter().GetResult();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<VoiceCraftServer>();
        serviceCollection.AddSingleton<McWssServer>();
        serviceCollection.AddSingleton<ServerProperties>();

        //Commands
        var rootCommand = new RootCommand();
        serviceCollection.AddSingleton(rootCommand);
        serviceCollection.AddSingleton<Command, SetPositionCommand>();
        serviceCollection.AddSingleton<Command, SetWorldIdCommand>();
        serviceCollection.AddSingleton<Command, ListCommand>();
        serviceCollection.AddSingleton<Command, SetTitleCommand>();
        serviceCollection.AddSingleton<Command, SetDescriptionCommand>();
        serviceCollection.AddSingleton<Command, SetNameCommand>();
        return serviceCollection.BuildServiceProvider();
    }
}