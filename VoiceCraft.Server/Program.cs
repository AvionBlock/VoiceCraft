using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Pages;

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
            serviceCollection.AddTransient<StartScreen>();
            return serviceCollection.BuildServiceProvider();
        }
    }
}