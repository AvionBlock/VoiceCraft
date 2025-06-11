using System.CommandLine;
using Jeek.Avalonia.Localization;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Commands;
using VoiceCraft.Server.Locales;

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
        serviceCollection.AddSingleton<ServerProperties>();

        //Commands
        var rootCommand = new RootCommand(Locales.Locales.Commands_Root_Description);
        serviceCollection.AddSingleton(rootCommand);
        serviceCollection.AddSingleton<Command, SetPropertyCommand>();
        serviceCollection.AddSingleton<Command, SetPositionCommand>();
        serviceCollection.AddSingleton<Command, SetWorldIdCommand>();
        serviceCollection.AddSingleton<Command, ListCommand>();
        serviceCollection.AddSingleton<Command, SetTitleCommand>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        foreach (var command in serviceProvider.GetServices<Command>())
        {
            rootCommand.AddCommand(command);
        }
        return serviceProvider;
    }
}