using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Telemetry;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server;

public static class App
{
    private static bool _shuttingDown;
    private static readonly CancellationTokenSource Cts = new();
    private static string? _bufferedCommand;

    public static async Task Start(RuntimeOptions runtimeOptions)
    {
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
        var telemetry = Program.ServiceProvider.GetRequiredService<ServerTelemetry>();

        try
        {
            //Startup.
            AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
            AnsiConsole.WriteLine(Localizer.Get("Startup.Starting"));

            //Properties
            properties.Load(runtimeOptions.ExitOnInvalidProperties);
            properties.ApplyRuntimeOverrides(runtimeOptions);
            telemetry.Configure(properties.TelemetryEnabled, properties.TelemetryToken);
            TelemetryTransport.FailureLogger = message => AnsiConsole.MarkupLine($"[yellow]{message.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine(properties.TelemetryEnabled
                ? "[grey]Telemetry is enabled. VoiceCraft sends anonymous startup, heartbeat, and crash diagnostics. Set \"TelemetryEnabled\": false in config/ServerProperties.json to disable it.[/]"
                : "[grey]Telemetry is disabled in config/ServerProperties.json.[/]");
            //Set locale if not overriden.
            if (!languageOverriden)
                Localizer.Instance.Language = properties.VoiceCraftConfig.Language;
            //Loaded, Set the title.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Starting")}";

            //Setup Audio Effects
            eventHandlerSystem.EnableVisibilityDisplay = properties.VoiceCraftConfig.EnableVisibilityDisplay;
            audioEffectSystem.DefaultAudioEffects = properties.DefaultAudioEffects;

            //Setup Servers
            liteNetServer.Config = properties.VoiceCraftConfig;
            mcWssMcApiServer.Config = properties.McWssConfig;
            httpMcApiServer.Config = properties.McHttpConfig;
            tcpMcApiServer.Config = properties.McTcpConfig;

            //Server Startup
            StartServer(liteNetServer);
            StartServer(httpMcApiServer);
            StartServer(tcpMcApiServer);
            StartServer(mcWssMcApiServer);

            //Server Started
            //Table for Server Setup Display
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

            //Register Commands
            AnsiConsole.WriteLine(Localizer.Get("Startup.Commands.Registering"));
            rootCommand.Description = Localizer.Get("Commands.RootCommand.Description");
            var commandCount = 0;
            foreach (var command in Program.ServiceProvider.GetServices<Command>())
            {
                rootCommand.Add(command);
                commandCount++;
            }

            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"Startup.Commands.Success:{commandCount}")}[/]");

            //Server finished.
            AnsiConsole.Write(serverSetupTable);
            AnsiConsole.MarkupLine($"[bold green]{Localizer.Get("Startup.Success")}[/]");
            AnsiConsole.MarkupLine("\0\0\0"); //This is here for docker images to detect server is running.
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: {Localizer.Get("Title.Running")}";
            await telemetry.ReportStartupAsync(CreateTelemetrySnapshot(
                liteNetServer,
                httpMcApiServer,
                tcpMcApiServer,
                mcWssMcApiServer));

            StartCommandTask();
            var startTime = DateTime.UtcNow;
            var lastTelemetryAt = DateTime.UtcNow;
            while (!Cts.IsCancellationRequested)
                try
                {
                    liteNetServer.Update();
                    httpMcApiServer.Update();
                    tcpMcApiServer.Update();
                    mcWssMcApiServer.Update();
                    visibilitySystem.Update();
                    eventHandlerSystem.Update();
                    await FlushCommand(rootCommand);
                    if (DateTime.UtcNow - lastTelemetryAt >= telemetry.GetHeartbeatInterval())
                    {
                        lastTelemetryAt = DateTime.UtcNow;
                        await telemetry.ReportHeartbeatAsync(CreateTelemetrySnapshot(
                            liteNetServer,
                            httpMcApiServer,
                            tcpMcApiServer,
                            mcWssMcApiServer));
                    }

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
            liteNetServer.Dispose();
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

    private static void StartServer(LiteNetVoiceCraftServer server)
    {
        try
        {
            AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Starting"));
            server.Start();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("VoiceCraftServer.Success")}[/]");
        }
        catch
        {
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
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("VoiceCraftServer.Success")}[/]");
        }
        catch
        {
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
        AnsiConsole.WriteLine(Localizer.Get("VoiceCraftServer.Stopped"));
    }

    private static void StopServer(McWssMcApiServer server)
    {
        if (!server.Config.Enabled) return;
        AnsiConsole.WriteLine(Localizer.Get("McWssServer.Stopping"));
        server.Stop();
        AnsiConsole.WriteLine(Localizer.Get("McWssServer.Stopped"));
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

    private static async Task FlushCommand(RootCommand rootCommand)
    {
        try
        {
            if (_bufferedCommand == null) return;
            var parseResult = rootCommand.Parse(_bufferedCommand);
            if (parseResult.Errors.Count == 0)
            {
                await parseResult.InvokeAsync();
                return;
            }

            AnsiConsole.MarkupLine($"[red]{Localizer.Get($"Commands.Exception:{_bufferedCommand}")}[/]");
            foreach (var parseError in parseResult.Errors)
            {
                AnsiConsole.MarkupLine($"[red]{parseError}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{Localizer.Get($"Commands.Exception:{_bufferedCommand}")}[/]");
            AnsiConsole.MarkupLine($"[red]{ex}[/]");
            LogService.Log(ex);
        }
        finally
        {
            _bufferedCommand = null;
        }
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

    private static ServerTelemetrySnapshot CreateTelemetrySnapshot(
        LiteNetVoiceCraftServer liteNetServer,
        HttpMcApiServer httpMcApiServer,
        TcpMcApiServer tcpMcApiServer,
        McWssMcApiServer mcWssMcApiServer)
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
}
