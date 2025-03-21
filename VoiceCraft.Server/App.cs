using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public static class App
    {
        private const int UpdateInterval = 20;
        
        private static bool _shutdown;

        public static void Start()
        {
            StartUpdateTask();
            var startScreen = Program.ServiceProvider.GetRequiredService<StartScreen>();
            if (startScreen.Start())
            {
                while (!_shutdown)
                {
                    Console.ReadLine();
                }
            }
            
            Shutdown(10);
        }

        private static void StartUpdateTask()
        {
            var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
            Task.Run(async () =>
            {
                var tick1 = Environment.TickCount;
                while (!_shutdown)
                {
                    try
                    {
                        server.Update();
                        var dist = Environment.TickCount - tick1;
                        var delay = UpdateInterval - dist;
                        if (delay > 0)
                            await Task.Delay(delay);
                        tick1 = Environment.TickCount;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteException(ex);
                    }
                }
            });
        }

        public static void Shutdown(uint delaySeconds = 0)
        {
            _shutdown = true;
            AnsiConsole.Status()
                .Start("Shutting down server...",ctx =>
                {
                    ctx.Spinner(Spinner.Known.GrowHorizontal);
                    while(delaySeconds > 0)
                    {
                        ctx.Status($"Shutting down server in {delaySeconds} seconds...");
                        Task.Delay(1000).Wait();
                        delaySeconds--;
                    }
                    
                    var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
                    server.Stop();
                    server.Dispose();
                    
                    AnsiConsole.MarkupLine("[green]Server shut down successfully![/]");
                });
        }
    }
}