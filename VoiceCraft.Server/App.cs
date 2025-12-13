using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Servers;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server;

public static class App
{
    private static bool _shuttingDown;
    private static readonly CancellationTokenSource Cts = new();
    private static string? _bufferedCommand;

    public static async Task Start()
    {
        //Servers
        var server = Program.ServiceProvider.GetRequiredService<VoiceCraftServer>();
        var mcWssServer = Program.ServiceProvider.GetRequiredService<McWssServer>();
        var httpServer = Program.ServiceProvider.GetRequiredService<McHttpServer>();
        //Systems
        var eventHandlerSystem = Program.ServiceProvider.GetRequiredService<EventHandlerSystem>();
        var visibilitySystem = Program.ServiceProvider.GetRequiredService<VisibilitySystem>();
        //Commands
        var rootCommand = Program.ServiceProvider.GetRequiredService<RootCommand>();
        //Other
        var properties = Program.ServiceProvider.GetRequiredService<ServerProperties>();

        try
        {
            //Startup.
            AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
            AnsiConsole.WriteLine(Localizer.Get("Startup.Starting"));

            //Properties
            properties.Load();
            //Set locale. May not set the first 2 messages, but it works.
            Localizer.Instance.Language = properties.VoiceCraftConfig.Language;
            //Loaded, Set the title.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Starting")}";

            //Server Startup
            server.Start(properties.VoiceCraftConfig);
            if (properties.McHttpConfig.Enabled)
                httpServer.Start(properties.McHttpConfig);
            if (properties.McWssConfig.Enabled)
                mcWssServer.Start(properties.McWssConfig);

            //Server Started
            //Table for Server Setup Display
            var serverSetupTable = new Table()
                .AddColumn(Localizer.Get("Tables.ServerSetup.Server"))
                .AddColumn(Localizer.Get("Tables.ServerSetup.Port"))
                .AddColumn(Localizer.Get("Tables.ServerSetup.Protocol"));

            serverSetupTable.AddRow("[green]VoiceCraft[/]", server.Config.Port.ToString(), "[aqua]UDP[/]");
            serverSetupTable.AddRow($"[{(properties.McHttpConfig.Enabled ? "green" : "red")}]McHttp[/]",
                httpServer.Config.Hostname, $"[{(properties.McHttpConfig.Enabled ? "aqua" : "red")}]TCP/HTTP[/]");
            serverSetupTable.AddRow($"[{(properties.McWssConfig.Enabled ? "green" : "red")}]McWss[/]",
                mcWssServer.Config.Hostname, $"[{(properties.McWssConfig.Enabled ? "aqua" : "red")}]TCP/WS[/]");

            //Register Commands
            AnsiConsole.WriteLine(Localizer.Get("Startup.Commands.Registering"));
            rootCommand.Description = Localizer.Get("Commands.RootCommand.Description");
            var commandCount = 0;
            foreach (var command in Program.ServiceProvider.GetServices<Command>())
            {
                rootCommand.AddCommand(command);
                commandCount++;
            }

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Startup.Commands.Success:{commandCount}")}[/]");

            //Server finished.
            AnsiConsole.Write(serverSetupTable);
            AnsiConsole.MarkupLine($"[bold green]{Localizer.Get("Startup.Success")}[/]");
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Running")}";

            StartCommandTask();
            var startTime = DateTime.UtcNow;
            while (!Cts.IsCancellationRequested)
                try
                {
                    server.Update();
                    httpServer.Update();
                    mcWssServer.Update();
                    visibilitySystem.Update();
                    eventHandlerSystem.Update();
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

            mcWssServer.Stop();
            httpServer.Stop();
            server.Stop();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("Shutdown.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{Localizer.Get("Startup.Failed")}[/]");
            AnsiConsole.WriteException(ex);
            Shutdown(10000);
        }
        finally
        {
            server.Dispose();
            Cts.Dispose();
        }
    }
    
    public static void Shutdown(uint delayMs = 0)
    {
        if (Cts.IsCancellationRequested || _shuttingDown) return;
        _shuttingDown = true;
        AnsiConsole.MarkupLine(delayMs > 0
            ? $"[bold yellow]{Localizer.Get($"Shutdown.StartingIn:{delayMs}")}[/]"
            : $"[bold yellow]{Localizer.Get("Shutdown.Starting")}[/]");
        Task.Delay((int)delayMs).Wait();
        Cts.Cancel();
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
            AnsiConsole.MarkupLine(
                $"[red]{Localizer.Get($"Commands.Exception:{_bufferedCommand}")}[/]");
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
}