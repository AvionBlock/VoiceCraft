using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server
{
    public static class App
    {
        private static bool _shuttingDown;
        private static CancellationTokenSource _cts = new();
        private static string? _bufferedCommand;

        public static async Task Start()
        {
            try
            {
                var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
                var rootCommand = Program.ServiceProvider.GetRequiredService<RootCommand>();

                //Startup.
                Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
                AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));

                //Properties
                AnsiConsole.WriteLine("Loading Server Properties...");
                var properties = ServerProperties.Load().VoiceCraftConfig;
                AnsiConsole.MarkupLine("[green]Successfully loaded server properties![/]");
                //Server Startup
                AnsiConsole.WriteLine("Starting VoiceCraft Server...");
                server.Config = properties;
                if (!server.Start())
                    throw new Exception("Failed to start VoiceCraft Server! Please check if another process is using the same port!");

                //Finish
                AnsiConsole.MarkupLine("[bold green]VoiceCraft server started![/]");

                StartCommandTask();
                var tick1 = Environment.TickCount;
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        server.Update();
                        await FlushCommand(rootCommand);
                        
                        var dist = Environment.TickCount - tick1;
                        var delay = Constants.FrameSizeMs - dist;
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
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]An error occurred while trying to startup the server![/]");
                AnsiConsole.WriteException(ex);
                Shutdown(10000);
            }
        }

        private static async Task FlushCommand(RootCommand rootCommand)
        {
            try
            {
                if (_bufferedCommand != null)
                    await rootCommand.InvokeAsync(_bufferedCommand);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]An error occurred while trying to execute the command {_bufferedCommand}![/]");
                AnsiConsole.WriteException(ex);
            }
            _bufferedCommand = null;
        }

        private static void StartCommandTask()
        {
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested && !_shuttingDown)
                {
                    if (_bufferedCommand != null)
                    {
                        await Task.Delay(1);
                        continue;
                    }
                    _bufferedCommand = Console.ReadLine();
                    if (_cts.IsCancellationRequested || _shuttingDown) return;
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