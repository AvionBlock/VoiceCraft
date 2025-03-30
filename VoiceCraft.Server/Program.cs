using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Commands;

namespace VoiceCraft.Server
{
    public static class Program
    {
        public static readonly IServiceProvider ServiceProvider = BuildServiceProvider();
        
        public static void Main(string[] args)
        {
            App.Start().GetAwaiter().GetResult();
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddSingleton<VoiceCraftServer>();
            
            //Commands
            var rootCommand = new RootCommand("VoiceCraft commands.");
            serviceCollection.AddSingleton(rootCommand);
            serviceCollection.AddSingleton<SetWorldIdCommand>();
            serviceCollection.AddSingleton<ListCommand>();
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            rootCommand.AddCommand(serviceProvider.GetRequiredService<SetWorldIdCommand>());
            rootCommand.AddCommand(serviceProvider.GetRequiredService<ListCommand>());
            return serviceProvider;
        }
    }
}