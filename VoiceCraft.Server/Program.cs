using System.CommandLine;
using Jeek.Avalonia.Localization;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Commands;
using VoiceCraft.Server.Locales;

namespace VoiceCraft.Server
{
    public static class Program
    {
        public static readonly IServiceProvider ServiceProvider = BuildServiceProvider();

        public static void Main(string[] args)
        {
            Localizer.SetLocalizer(new EmbeddedJsonLocalizer("VoiceCraft.Server.Locales"));
            Localizer.Language = "en-us";
            //Localizer.Language = "nl-nl";
            App.Start().GetAwaiter().GetResult();
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddSingleton<VoiceCraftServer>();
            
            //Commands
            var rootCommand = new RootCommand(Locales.Locales.Commands_Root_Description);
            serviceCollection.AddSingleton(rootCommand);
            serviceCollection.AddSingleton<SetPropertyCommand>();
            serviceCollection.AddSingleton<SetPositionCommand>();
            serviceCollection.AddSingleton<SetWorldIdCommand>();
            serviceCollection.AddSingleton<ListCommand>();
            serviceCollection.AddSingleton<SetTitleCommand>();
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            rootCommand.AddCommand(serviceProvider.GetRequiredService<SetPropertyCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<SetPositionCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<SetWorldIdCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<ListCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<SetTitleCommand>());
            return serviceProvider;
        }
    }
}