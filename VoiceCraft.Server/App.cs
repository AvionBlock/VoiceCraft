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
        private static readonly CancellationTokenSource Cts = new();
        private static string? _bufferedCommand;

        public static async Task Start()
        {
            try
            {
                var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
                var rootCommand = Program.ServiceProvider.GetRequiredService<RootCommand>();

                //Startup.
                Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Locales.Locales.Startup_Title_Starting}";
                AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
                
                //Table for Server Setup Display
                var serverSetupTable = new Table()
                    .AddColumn(Locales.Locales.Startup_ServerSetupTable_Server)
                    .AddColumn(Locales.Locales.Startup_ServerSetupTable_Port)
                    .AddColumn(Locales.Locales.Startup_ServerSetupTable_Protocol);

                //Properties
                AnsiConsole.WriteLine(Locales.Locales.Startup_ServerProperties_Loading);
                var properties = ServerProperties.Load().VoiceCraftConfig;
                AnsiConsole.MarkupLine("[green]" + Locales.Locales.Startup_ServerProperties_Success + "[/]");
                //Server Startup
                AnsiConsole.WriteLine(Locales.Locales.Startup_VoiceCraftServer_Starting);
                server.Config = properties;
                if (!server.Start())
                    throw new Exception(Locales.Locales.Startup_VoiceCraftServer_Failed);
                
                //Server Started
                AnsiConsole.MarkupLine("[green]" + Locales.Locales.Startup_VoiceCraftServer_Success + "[/]");
                serverSetupTable.AddRow("[green]VoiceCraft[/]", server.Config.Port.ToString(), "[aqua]UDP[/]");
                
                //Server finished.
                AnsiConsole.MarkupLine("[bold green]" + Locales.Locales.Startup_Finished + "[/]");
                AnsiConsole.Write(serverSetupTable);

                StartCommandTask();
                var startTime = DateTime.UtcNow;
                while (!Cts.IsCancellationRequested)
                {
                    try
                    {
                        server.Update();
                        await FlushCommand(rootCommand);
                        
                        var dist = DateTime.UtcNow - startTime;
                        var delay = Constants.TickRate - dist.TotalMilliseconds;
                        if (delay > 0)
                            await Task.Delay((int)delay);
                        startTime = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteException(ex);
                    }
                }

                server.Stop();
                server.Dispose();
                Cts.Dispose();
                AnsiConsole.MarkupLine("[green]" + Locales.Locales.Shutdown_Success + "[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]" + Locales.Locales.Startup_Exception + "[/]");
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
                AnsiConsole.MarkupLine($"[red]{string.Format(Locales.Locales.Command_Exception, _bufferedCommand)}[/]");
                AnsiConsole.WriteException(ex);
            }
            _bufferedCommand = null;
        }

        private static void StartCommandTask()
        {
            Task.Run(async () =>
            {
                while (!Cts.IsCancellationRequested && !_shuttingDown)
                {
                    if (_bufferedCommand != null)
                    {
                        await Task.Delay(1);
                        continue;
                    }
                    _bufferedCommand = Console.ReadLine();
                    if (Cts.IsCancellationRequested || _shuttingDown) return;
                }
            });
        }

        private static void Shutdown(uint delayMs = 0)
        {
            if (Cts.IsCancellationRequested || _shuttingDown) return;
            _shuttingDown = true;
            AnsiConsole.MarkupLine(delayMs > 0 ? $"[bold yellow]{string.Format(Locales.Locales.Shutdown_StartingIn, delayMs)}[/]" : $"[bold yellow]{Locales.Locales.Shutdown_Starting}[/]");
            Task.Delay((int)delayMs).Wait();
            Cts.Cancel();
        }
    }
}