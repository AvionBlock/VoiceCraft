using System.CommandLine;
using Jeek.Avalonia.Localization;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server;

public static class App
{
    private static bool _shuttingDown;
    private static readonly CancellationTokenSource Cts = new();
    private static string? _bufferedCommand;

    public static async Task Start()
    {
        var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
        var rootCommand = Program.ServiceProvider.GetRequiredService<RootCommand>();
        var properties = Program.ServiceProvider.GetRequiredService<ServerProperties>();
        
        try
        {
            //Startup.
            AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
            AnsiConsole.WriteLine(Locales.Locales.Startup_Starting);

            //Properties
            properties.Load();
            Localizer.Language = properties.VoiceCraftConfig.Language; //Set locale. May not set the first 2 messages, but it works.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Locales.Locales.Title_Starting}"; //Loaded, Set the title.

            //Server Startup
            server.Start(properties.VoiceCraftConfig);

            //Server Started
            //Table for Server Setup Display
            var serverSetupTable = new Table()
                .AddColumn(Locales.Locales.Tables_ServerSetup_Server)
                .AddColumn(Locales.Locales.Tables_ServerSetup_Port)
                .AddColumn(Locales.Locales.Tables_ServerSetup_Protocol);

            serverSetupTable.AddRow("[green]VoiceCraft[/]", server.Config.Port.ToString(), "[aqua]UDP[/]");

            //Register Commands
            AnsiConsole.WriteLine(Locales.Locales.Startup_Commands_Registering);
            var commandCount = 0;
            foreach (var command in Program.ServiceProvider.GetServices<Command>())
            {
                rootCommand.AddCommand(command);
                commandCount++;
            }

            AnsiConsole.MarkupLine($"[green]{Locales.Locales.Startup_Commands_Success.Replace("{commands}", commandCount.ToString())}[/]");

            //Server finished.
            AnsiConsole.Write(serverSetupTable);
            AnsiConsole.MarkupLine($"[bold green]{Locales.Locales.Startup_Success}[/]");

            StartCommandTask();
            var startTime = DateTime.UtcNow;
            while (!Cts.IsCancellationRequested)
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

            server.Stop();
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.Shutdown_Success}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{Locales.Locales.Startup_Failed}[/]");
            AnsiConsole.WriteException(ex);
            Shutdown(10000);
        }
        finally
        {
            server.Dispose();
            Cts.Dispose();
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
            AnsiConsole.MarkupLine($"[red]{Locales.Locales.Commands_Exception.Replace("{commandName}", _bufferedCommand)}[/]");
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
        AnsiConsole.MarkupLine(delayMs > 0
            ? $"[bold yellow]{Locales.Locales.Shutdown_StartingIn.Replace("{delayMs}", delayMs.ToString())}[/]"
            : $"[bold yellow]{Locales.Locales.Shutdown_Starting}[/]");
        Task.Delay((int)delayMs).Wait();
        Cts.Cancel();
    }
}