using System.Net;
using OpenPort.Net;
using OpenPort.Net.Models;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Server.Services;

public sealed class PortMappingService
{
    private readonly List<OpenPortLease> _leases = [];
    private readonly HashSet<string> _opened = new(StringComparer.OrdinalIgnoreCase);

    public async Task OpenAsync(
        LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig voiceCraftConfig,
        HttpMcApiServer.HttpMcApiConfig httpConfig,
        TcpMcApiServer.McTcpConfig tcpConfig,
        McWssMcApiServer.McWssMcApiConfig wssConfig)
    {
        if (voiceCraftConfig.AutoOpenPort)
        {
            var internalPort = (int)voiceCraftConfig.Port;
            await OpenPortMappingAsync(
                "VoiceCraft",
                PortProtocol.Udp,
                internalPort,
                GetExternalPort(voiceCraftConfig.ExternalPort, internalPort),
                GetLifetime(voiceCraftConfig.PortMappingLifetimeMinutes),
                GetTimeout(voiceCraftConfig.PortMappingTimeoutSeconds));
        }

        if (httpConfig is { Enabled: true, AutoOpenPort: true } &&
            TryGetUriPortAndHost(httpConfig.Hostname, out var httpPort, out var httpHost) &&
            ShouldOpenTransportPort("McHttp", httpHost))
        {
            await OpenPortMappingAsync(
                "McHttp",
                PortProtocol.Tcp,
                httpPort,
                GetExternalPort(httpConfig.ExternalPort, httpPort),
                GetLifetime(httpConfig.PortMappingLifetimeMinutes),
                GetTimeout(httpConfig.PortMappingTimeoutSeconds));
        }

        if (tcpConfig is { Enabled: true, AutoOpenPort: true } && ShouldOpenTransportPort("McTcp", tcpConfig.Hostname))
        {
            await OpenPortMappingAsync(
                "McTcp",
                PortProtocol.Tcp,
                tcpConfig.Port,
                GetExternalPort(tcpConfig.ExternalPort, tcpConfig.Port),
                GetLifetime(tcpConfig.PortMappingLifetimeMinutes),
                GetTimeout(tcpConfig.PortMappingTimeoutSeconds));
        }

        if (wssConfig is { Enabled: true, AutoOpenPort: true } &&
            TryGetUriPortAndHost(wssConfig.Hostname, out var wssPort, out var wssHost) &&
            ShouldOpenTransportPort("McWss", wssHost))
        {
            await OpenPortMappingAsync(
                "McWss",
                PortProtocol.Tcp,
                wssPort,
                GetExternalPort(wssConfig.ExternalPort, wssPort),
                GetLifetime(wssConfig.PortMappingLifetimeMinutes),
                GetTimeout(wssConfig.PortMappingTimeoutSeconds));
        }
    }

    public async Task CloseAsync()
    {
        if (_leases.Count == 0)
            return;

        foreach (var lease in _leases.AsEnumerable().Reverse())
        {
            try
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"PortMapper.Closing:{lease.Mapping.InternalPort},{lease.Mapping.Protocol}")}[/]");
                await lease.DisposeAsync();
                AnsiConsole.MarkupLine(
                    $"[green]{Localizer.Get($"PortMapper.Closed:{lease.Mapping.InternalPort},{lease.Mapping.Protocol}")}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                LogService.Log(ex);
                AnsiConsole.MarkupLine(
                    $"[yellow]{Localizer.Get($"PortMapper.Exceptions.Close:{lease.Mapping.InternalPort},{lease.Mapping.Protocol}")}[/]");
            }
        }

        _leases.Clear();
        _opened.Clear();
    }

    private async Task OpenPortMappingAsync(
        string name,
        PortProtocol protocol,
        int internalPort,
        int externalPort,
        TimeSpan lifetime,
        TimeSpan timeout)
    {
        var key = $"{protocol}:{internalPort}:{externalPort}";
        if (!_opened.Add(key))
            return;

        try
        {
            AnsiConsole.MarkupLine(
                $"[yellow]{Localizer.Get($"PortMapper.Opening:{name},{internalPort},{protocol}")}[/]");

            var client = new OpenPortClient(new OpenPortOptions
            {
                Timeout = timeout
            });

            var lease = await client.OpenLeaseAsync(new PortMapping
            {
                InternalPort = internalPort,
                ExternalPort = externalPort,
                Protocol = protocol,
                Description = $"{name} Server",
                Lifetime = lifetime
            });

            var mappedPort = lease.Result.ExternalPort ?? lease.Mapping.ExternalPort;
            var mappedAddress = lease.Result.ExternalAddress?.ToString() ?? "N.A.";
            AnsiConsole.MarkupLine(
                $"[green]{Localizer.Get($"PortMapper.Opened:{name},{internalPort},{protocol},{lease.Result.Provider},{mappedAddress},{mappedPort}")}[/]");
            _leases.Add(lease);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            LogService.Log(ex);
            AnsiConsole.MarkupLine(
                $"[yellow]{Localizer.Get($"PortMapper.Exceptions.Open:{name},{internalPort},{protocol}")}[/]");
        }
    }

    private static bool TryGetUriPortAndHost(string configuredHostname, out int port, out string host)
    {
        port = 0;
        host = string.Empty;
        if (!Uri.TryCreate(configuredHostname, UriKind.Absolute, out var uri))
            return false;

        port = uri.IsDefaultPort ? GetDefaultPort(uri.Scheme) : uri.Port;
        host = uri.Host;
        return port is >= 1 and <= 65535;
    }

    private static int GetDefaultPort(string scheme)
    {
        return scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               scheme.Equals("wss", StringComparison.OrdinalIgnoreCase)
            ? 443
            : 80;
    }

    private static bool ShouldOpenTransportPort(string name, string host)
    {
        if (host is "*" or "+" or "0.0.0.0" or "::")
            return true;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address))
            return true;

        AnsiConsole.MarkupLine(
            $"[yellow]{Localizer.Get($"PortMapper.SkippedLoopback:{name},{host}")}[/]");
        return false;
    }

    private static int GetExternalPort(uint externalPort, int internalPort)
    {
        return externalPort == 0 ? internalPort : checked((int)externalPort);
    }

    private static TimeSpan GetLifetime(uint lifetimeMinutes)
    {
        return TimeSpan.FromMinutes(Math.Max(1, lifetimeMinutes));
    }

    private static TimeSpan GetTimeout(uint timeoutSeconds)
    {
        return TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds));
    }
}