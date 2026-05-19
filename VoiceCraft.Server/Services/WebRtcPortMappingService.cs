using System.Net;
using OpenPort.Net;
using OpenPort.Net.Models;
using Spectre.Console;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Server.Services;

public sealed class WebRtcPortMappingService : IAsyncDisposable, IDisposable
{
    private readonly List<OpenPortLease> _leases = [];
    private readonly List<WebRtcVoiceCraftServer.WebRtcExternalIceCandidateMapping> _externalIceCandidateMappings = [];
    private OpenPortClient? _client;
    private bool _disposed;

    public IReadOnlyList<WebRtcVoiceCraftServer.WebRtcExternalIceCandidateMapping> ExternalIceCandidateMappings =>
        _externalIceCandidateMappings;

    public async Task OpenAsync(
        WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig config,
        CancellationToken cancellationToken = default)
    {
        if (!config.Enabled || config.PortMapping is not { Enabled: true } portMappingConfig)
            return;

        DisposeLeases();
        _externalIceCandidateMappings.Clear();

        _client = new OpenPortClient(new OpenPortOptions
        {
            Timeout = TimeSpan.FromMilliseconds(Math.Max(portMappingConfig.TimeoutMs, 1000))
        });

        var lifetime = TimeSpan.FromMinutes(Math.Max(portMappingConfig.LifetimeMinutes, 1));
        var externalAddress = TryParsePublicAddress(portMappingConfig);

        if (portMappingConfig.MapSignalingPort)
            await OpenSignalingMappingAsync(config, lifetime, portMappingConfig, cancellationToken)
                .ConfigureAwait(false);

        if (portMappingConfig.MapUdpPortRange)
        {
            var results = await OpenUdpRangeMappingsAsync(config, lifetime, portMappingConfig, cancellationToken)
                .ConfigureAwait(false);

            externalAddress ??= results
                .Select(x => x.Result.ExternalAddress)
                .FirstOrDefault(x => x is not null);
            externalAddress ??= await _client.GetExternalIPAddressAsync(cancellationToken).ConfigureAwait(false);
            if (externalAddress is null)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]WebRTC automatic port mapping opened UDP ports, but the router did not report an external address. Public ICE candidates were not added.[/]");
                return;
            }

            foreach (var lease in results)
            {
                _externalIceCandidateMappings.Add(new WebRtcVoiceCraftServer.WebRtcExternalIceCandidateMapping
                {
                    InternalPort = lease.Mapping.InternalPort,
                    ExternalAddress = externalAddress.ToString(),
                    ExternalPort = lease.Mapping.ExternalPort
                });
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        var leases = _leases.ToArray();
        _leases.Clear();
        _externalIceCandidateMappings.Clear();

        foreach (var lease in leases)
        {
            try
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogService.Log(ex);
            }
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task OpenSignalingMappingAsync(
        WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig config,
        TimeSpan lifetime,
        WebRtcVoiceCraftServer.WebRtcPortMappingConfig portMappingConfig,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(config.SignalingUrl, UriKind.Absolute, out var uri) ||
            uri.Port is < 1 or > 65535)
        {
            HandleMappingFailure(
                "WebRTC signaling URL does not contain a valid TCP port for automatic port mapping.",
                portMappingConfig,
                null);
            return;
        }

        await OpenLeaseAsync(
                uri.Port,
                uri.Port,
                PortProtocol.Tcp,
                lifetime,
                portMappingConfig,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<OpenPortLease>> OpenUdpRangeMappingsAsync(
        WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig config,
        TimeSpan lifetime,
        WebRtcVoiceCraftServer.WebRtcPortMappingConfig portMappingConfig,
        CancellationToken cancellationToken)
    {
        if (config.PortRangeStart is not >= 1 or > 65535 ||
            config.PortRangeEnd is not >= 1 or > 65535 ||
            config.PortRangeEnd < config.PortRangeStart)
        {
            HandleMappingFailure(
                "WebRTC UDP port range is invalid and cannot be mapped automatically.",
                portMappingConfig,
                null);
            return [];
        }

        var openedLeases = new List<OpenPortLease>();
        for (var port = config.PortRangeStart.Value; port <= config.PortRangeEnd.Value; port++)
        {
            var lease = await OpenLeaseAsync(
                    port,
                    port,
                    PortProtocol.Udp,
                    lifetime,
                    portMappingConfig,
                    cancellationToken)
                .ConfigureAwait(false);
            if (lease != null)
                openedLeases.Add(lease);
        }

        return openedLeases;
    }

    private async Task<OpenPortLease?> OpenLeaseAsync(
        int internalPort,
        int externalPort,
        PortProtocol protocol,
        TimeSpan lifetime,
        WebRtcVoiceCraftServer.WebRtcPortMappingConfig portMappingConfig,
        CancellationToken cancellationToken)
    {
        if (_client == null)
            throw new InvalidOperationException("OpenPort.Net client has not been initialized.");

        try
        {
            var lease = await _client.OpenLeaseAsync(
                    new PortMapping
                    {
                        InternalPort = internalPort,
                        ExternalPort = externalPort,
                        Protocol = protocol,
                        Description = "VoiceCraft WebRTC",
                        Lifetime = lifetime
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _leases.Add(lease);
            if (lease.Result.Status == OpenPortStatus.ExternalPortChanged)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]WebRTC {protocol} port mapping changed external port {externalPort} to {lease.Mapping.ExternalPort}.[/]");
            }

            return lease;
        }
        catch (Exception ex)
        {
            HandleMappingFailure(
                $"Failed to map WebRTC {protocol} port {internalPort}.",
                portMappingConfig,
                ex);
            return null;
        }
    }

    private static IPAddress? TryParsePublicAddress(
        WebRtcVoiceCraftServer.WebRtcPortMappingConfig portMappingConfig)
    {
        var publicAddress = portMappingConfig.PublicAddress;
        if (string.IsNullOrWhiteSpace(publicAddress))
            return null;

        if (IPAddress.TryParse(publicAddress, out var ipAddress))
            return ipAddress;

        HandleMappingFailure(
            "WebRTC PortMapping.PublicAddress must be an IPv4 or IPv6 address.",
            portMappingConfig,
            null);
        return null;
    }

    private static void HandleMappingFailure(
        string message,
        WebRtcVoiceCraftServer.WebRtcPortMappingConfig portMappingConfig,
        Exception? exception)
    {
        if (portMappingConfig.FailOnFailure)
            throw new InvalidOperationException(message, exception);

        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
        if (exception != null)
            LogService.Log(exception);
    }

    private void DisposeLeases()
    {
        foreach (var lease in _leases.ToArray())
        {
            try
            {
                lease.Dispose();
            }
            catch (Exception ex)
            {
                LogService.Log(ex);
            }
        }

        _leases.Clear();
    }
}
