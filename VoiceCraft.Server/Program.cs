using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public static class Program
    {
        private static readonly App App = new(BuildServiceProvider());
        
        public static void Main(string[] args)
        {
            App.Start().GetAwaiter().GetResult();
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddSingleton<VoiceCraftServer>();
            serviceCollection.AddTransient<StartScreen>();
            return serviceCollection.BuildServiceProvider();
        }
    }
}