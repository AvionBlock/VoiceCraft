using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public static class App
    {
        private static bool _shuttingDown;
        private static CancellationTokenSource _cts = new();

        public static async Task Start()
        {
            var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();

            StartCommandTask();
            var startScreen = Program.ServiceProvider.GetRequiredService<StartScreen>();
            startScreen.Start();
            
            var tick1 = Environment.TickCount;
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    server.Update();
                    var dist = Environment.TickCount - tick1;
                    var delay = Constants.UpdateIntervalMs - dist;
                    if (delay > 0)
                        await Task.Delay(delay);
                    tick1 = Environment.TickCount;
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }

            server.Stop();
            server.Dispose();
            _cts.Dispose();
            AnsiConsole.MarkupLine("[green]Server shut down successfully![/]");
        }

        private static void StartCommandTask()
        {
            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested && !_shuttingDown)
                {
                    var result = Console.ReadLine();
                    if (_cts.IsCancellationRequested || _shuttingDown) return;
                    AnsiConsole.WriteLine($"Said {result}");
                }
            });
        }

        public static void Shutdown(uint delayMs = 0)
        {
            if (_cts.IsCancellationRequested || _shuttingDown) return;
            _shuttingDown = true;
            AnsiConsole.MarkupLine(delayMs > 0 ? $"[bold yellow]Shutting down server in {delayMs}ms...[/]" : $"[bold yellow]Shutting down server...[/]");
            Task.Delay((int)delayMs).Wait();
            _cts.Cancel();
        }
    }
}