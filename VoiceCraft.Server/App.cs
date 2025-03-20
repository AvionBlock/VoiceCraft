using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public class App(IServiceProvider serviceProvider)
    {
        private const int UpdateInterval = 20;
        
        private bool _isRunning;
        private bool _shutdown;

        public async Task Start()
        {
            _ = StartScreen();
            var server = serviceProvider.GetRequiredService<VoiceCraftServer>();

            var tick1 = Environment.TickCount;
            while (!_shutdown)
            {
                server.Update();
                var dist = Environment.TickCount - tick1;
                var delay = UpdateInterval - dist;
                if(delay > 0)
                    await Task.Delay(delay);
                tick1 = Environment.TickCount;
            }
            
            server.Stop();
        }

        private async Task StartScreen()
        {
            var startScreen = serviceProvider.GetRequiredService<StartScreen>();
            await startScreen.Start();
        }
    }
}