using System.Collections.Concurrent;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Server.Services;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server;

public class App
{
    private static bool _shuttingDown;
    private static readonly CancellationTokenSource Cts = new();
    private static readonly ConcurrentQueue<string> QueuedCommands = new();
    private static readonly SemaphoreSlim TelemetrySemaphore = new(1, 1);

    public static async Task Start(RuntimeOptions runtimeOptions)
    {
        if (runtimeOptions.DisableAnsi)
            AnsiConsole.Console.Profile.Capabilities.Ansi = false;
        if (runtimeOptions.DisableColor)
            AnsiConsole.Console.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;

        var languageOverriden = !string.IsNullOrWhiteSpace(runtimeOptions.Language);
        //Set language if overriden.
        if (languageOverriden)
            Localizer.Instance.Language = runtimeOptions.Language ?? "en-US";

        //Servers
        var liteNetServer = Program.ServiceProvider.GetRequiredService<LiteNetVoiceCraftServer>();
        var mcWssMcApiServer = Program.ServiceProvider.GetRequiredService<McWssMcApiServer>();
        var httpMcApiServer = Program.ServiceProvider.GetRequiredService<HttpMcApiServer>();
        var tcpMcApiServer = Program.ServiceProvider.GetRequiredService<TcpMcApiServer>();
        //Systems
        var eventHandlerSystem = Program.ServiceProvider.GetRequiredService<EventHandlerSystem>();
        var visibilitySystem = Program.ServiceProvider.GetRequiredService<VisibilitySystem>();
        var audioEffectSystem = Program.ServiceProvider.GetRequiredService<AudioEffectSystem>();
        //Commands
        var rootCommand = Program.ServiceProvider.GetRequiredService<RootCommand>();
        //Other
        var properties = Program.ServiceProvider.GetRequiredService<ServerProperties>();
        var telemetry = Program.ServiceProvider.GetRequiredService<ServerTelemetryService>();
        var portMappingService = Program.ServiceProvider.GetRequiredService<PortMappingService>();

        try
        {
            //Startup.
            AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
            AnsiConsole.WriteLine(Localizer.Get("Startup.Starting"));

            //Properties
            properties.Load(runtimeOptions);
            AnsiConsole.MarkupLine(properties.TelemetryEnabled
                ? "[aqua]Telemetry is enabled. VoiceCraft sends anonymous startup, heartbeat, and crash diagnostics. Set \"TelemetryEnabled\": false in config/ServerProperties.json to disable it.[/]"
                : "[aqua]Telemetry is disabled in config/ServerProperties.json.[/]");
            //Set locale if not overriden.
            if (!languageOverriden)
                Localizer.Instance.Language = properties.VoiceCraftConfig.Language;
            //Loaded, Set the title.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Starting")}";

            //Setup Audio Effects
            eventHandlerSystem.EnableVisibilityDisplay = properties.VoiceCraftConfig.EnableVisibilityDisplay;
            audioEffectSystem.DefaultAudioEffects = properties.DefaultAudioEffects;

            //Setup Server Configs
            liteNetServer.Config = properties.VoiceCraftConfig;
            mcWssMcApiServer.Config = properties.McWssConfig;
            httpMcApiServer.Config = properties.McHttpConfig;
            tcpMcApiServer.Config = properties.McTcpConfig;

            //Server Startup
            StartServer(liteNetServer);
            StartServer(httpMcApiServer);
            StartServer(tcpMcApiServer);
            StartServer(mcWssMcApiServer);
            RegisterCommands(runtimeOptions, rootCommand);

            //Open Ports
            await portMappingService.OpenAsync(
                liteNetServer.Config,
                httpMcApiServer.Config,
                tcpMcApiServer.Config,
                mcWssMcApiServer.Config);

            //Server started.
            AnsiConsole.Write(CreateConfigurationTable(
                liteNetServer,
                httpMcApiServer,
                tcpMcApiServer,
                mcWssMcApiServer));
            AnsiConsole.MarkupLine($"[bold green]{Localizer.Get("Startup.Success")}[/]");
            AnsiConsole.MarkupLine("\0\0\0"); //This is here for docker images to detect server is running.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Running")}";
            await telemetry.ReportStartupAsync(CreateTelemetrySnapshot(
                liteNetServer,
                httpMcApiServer,
                tcpMcApiServer,
                mcWssMcApiServer));

            StartCommandTask(runtimeOptions);
            StartTelemetryTask(telemetry, properties, liteNetServer, httpMcApiServer, tcpMcApiServer, mcWssMcApiServer);
            var startTime = DateTime.UtcNow;
            while (!Cts.IsCancellationRequested)
            {
                await TelemetrySemaphore.WaitAsync();
                try
                {
                    liteNetServer.Update();
                    httpMcApiServer.Update();
                    tcpMcApiServer.Update();
                    mcWssMcApiServer.Update();
                    visibilitySystem.Update();
                    eventHandlerSystem.Update();
                    await NextCommandAsync(rootCommand);

                    var dist = DateTime.UtcNow - startTime;
                    var delay = Constants.TickRate - dist.TotalMilliseconds;
                    if (delay > 0)
                        await Task.Delay((int)delay);
                    startTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]{ex}[/]");
                }
                finally
                {
                    TelemetrySemaphore.Release();
                }
            }

            StopServer(liteNetServer);
            StopServer(httpMcApiServer);
            StopServer(tcpMcApiServer);
            StopServer(mcWssMcApiServer);
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("Shutdown.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{Localizer.Get("Startup.Failed")}[/]");
            AnsiConsole.MarkupLine($"[red]{ex}[/]");
            Shutdown(10000);
            LogService.Log(ex);
        }
        finally
        {
            await portMappingService.CloseAsync();
            liteNetServer.Dispose();
            httpMcApiServer.Dispose();
            tcpMcApiServer.Dispose();
            mcWssMcApiServer.Dispose();
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

    private static Table CreateConfigurationTable(
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        TcpMcApiServer tcpMcApiServer,
        McWssMcApiServer mcWssMcApiServer)
    {
        var serverSetupTable = new Table()
            .AddColumn(Localizer.Get("Tables.ServerSetup.Server"))
            .AddColumn(Localizer.Get("Tables.ServerSetup.Port"))
            .AddColumn(Localizer.Get("Tables.ServerSetup.Protocol"));

        serverSetupTable.AddRow(
            "[green]VoiceCraft[/]",
            liteNetServer.Config.Port.ToString(),
            "[aqua]UDP[/]");
        serverSetupTable.AddRow(
            $"[{(httpMcApiServer.Config.Enabled ? "green" : "red")}]McHttp[/]",
            httpMcApiServer.Config.Enabled ? httpMcApiServer.Config.Hostname : "[red]-[/]",
            $"[{(httpMcApiServer.Config.Enabled ? "aqua" : "red")}]TCP/HTTP[/]");
        serverSetupTable.AddRow(
            $"[{(tcpMcApiServer.Config.Enabled ? "green" : "red")}]McTcp[/]",
            tcpMcApiServer.Config.Enabled
                ? $"{tcpMcApiServer.Config.Hostname}:{tcpMcApiServer.Config.Port}"
                : "[red]-[/]",
            $"[{(tcpMcApiServer.Config.Enabled ? "aqua" : "red")}]TCP[/]");
        serverSetupTable.AddRow(
            $"[{(mcWssMcApiServer.Config.Enabled ? "green" : "red")}]McWss[/]",
            mcWssMcApiServer.Config.Enabled ? mcWssMcApiServer.Config.Hostname : "[red]-[/]",
            $"[{(mcWssMcApiServer.Config.Enabled ? "aqua" : "red")}]TCP/WS[/]");

        return serverSetupTable;
    }

    private static void StartServer(LiteNetVoiceCraftServer server)
    {
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Starting"));
            server.Start();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("VoiceCraftServer.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            LogService.Log(ex);
            throw new Exception(Localizer.Get("VoiceCraftServer.Exceptions.Failed"));
        }
    }

    private static void StartServer(McWssMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("McWssServer.Starting"));
            server.Start();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("McWssServer.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            LogService.Log(ex);
            throw new Exception(Localizer.Get("McWssServer.Exceptions.Failed"));
        }
    }

    private static void StartServer(HttpMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("McHttpServer.Starting"));
            server.Start();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("McHttpServer.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            LogService.Log(ex);
            throw new Exception(Localizer.Get("McHttpServer.Exceptions.Failed"));
        }
    }

    private static void StartServer(TcpMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("McTcpServer.Starting"));
            server.Start();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("McTcpServer.Success")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            LogService.Log(ex);
            throw new Exception(Localizer.Get("McTcpServer.Exceptions.Failed"));
        }
    }

    private static void StopServer(LiteNetVoiceCraftServer server)
    {
        AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Stopping"));
        server.Stop();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("VoiceCraftServer.Stopped")}[/]");
    }

    private static void StopServer(McWssMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        AnsiConsole.WriteLine(Localizer.Get("McWssServer.Stopping"));
        server.Stop();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("McWssServer.Stopped")}[/]");
    }

    private static void StopServer(HttpMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        AnsiConsole.WriteLine(Localizer.Get("McHttpServer.Stopping"));
        server.Stop();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("McHttpServer.Stopped")}[/]");
    }

    private static void StopServer(TcpMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        AnsiConsole.WriteLine(Localizer.Get("McTcpServer.Stopping"));
        server.Stop();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("McTcpServer.Stopped")}[/]");
    }

    private static void RegisterCommands(RuntimeOptions options, RootCommand rootCommand)
    {
        if (options.DisableCommands) return;
        AnsiConsole.WriteLine(Localizer.Get("Startup.Commands.Registering"));
        rootCommand.Description = Localizer.Get("Commands.RootCommand.Description");
        var commandCount = 0;
        foreach (var command in Program.ServiceProvider.GetServices<Command>())
        {
            rootCommand.Add(command);
            commandCount++;
        }

        AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Startup.Commands.Success:{commandCount}")}[/]");
    }

    private static void StartCommandTask(RuntimeOptions options)
    {
        if (options.DisableCommands) return;
        Task.Run(() =>
        {
            string? command = null;
            while (!Cts.IsCancellationRequested && !_shuttingDown)
            {
                if (!string.IsNullOrWhiteSpace(command))
                {
                    QueuedCommands.Enqueue(command);
                }

                command = Console.ReadLine();
            }
        });
    }

    private static async Task NextCommandAsync(RootCommand rootCommand)
    {
        if (!QueuedCommands.TryDequeue(out var command)) return;
        try
        {
            var parseResult = rootCommand.Parse(command);
            if (parseResult.Errors.Count == 0)
            {
                await parseResult.InvokeAsync();
                return;
            }

            AnsiConsole.MarkupLine($"[red]{Localizer.Get($"Commands.Exception:{command}")}[/]");
            foreach (var parseError in parseResult.Errors)
            {
                AnsiConsole.MarkupLine($"[red]{parseError}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{Localizer.Get($"Commands.Exception:{command}")}[/]");
            AnsiConsole.MarkupLine($"[red]{ex}[/]");
            LogService.Log(ex);
        }
    }

    private static void StartTelemetryTask(
        ServerTelemetryService telemetry,
        ServerProperties properties,
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        TcpMcApiServer tcpMcApiServer,
        McWssMcApiServer mcWssMcApiServer)
    {
        if (!properties.TelemetryEnabled) return;
        Task.Run(async () =>
        {
            while (!Cts.IsCancellationRequested && !_shuttingDown)
            {
                await telemetry.ReportHeartbeatAsync(CreateTelemetrySnapshot(
                    liteNetServer,
                    httpMcApiServer,
                    tcpMcApiServer,
                    mcWssMcApiServer));
                await Task.Delay(ServerTelemetryService.HeartbeatInterval);
            }
        });
    }

    private static ServerTelemetrySnapshot CreateTelemetrySnapshot(
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        TcpMcApiServer tcpMcApiServer,
        McWssMcApiServer mcWssMcApiServer)
    {
        TelemetrySemaphore.Wait();
        try
        {
            return new ServerTelemetrySnapshot
            {
                Version = VoiceCraftServer.Version.ToString(),
                Language = Localizer.Instance.Language,
                PositioningType = liteNetServer.Config.PositioningType.ToString(),
                EnableVisibilityDisplay = liteNetServer.Config.EnableVisibilityDisplay,
                McHttpEnabled = httpMcApiServer.Config.Enabled,
                McTcpEnabled = tcpMcApiServer.Config.Enabled,
                McWssEnabled = mcWssMcApiServer.Config.Enabled,
                ConnectedClients = liteNetServer.ConnectedPeers
            };
        }
        finally
        {
            TelemetrySemaphore.Release();
        }
    }
}