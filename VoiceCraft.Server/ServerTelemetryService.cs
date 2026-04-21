using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using VoiceCraft.Core;
using VoiceCraft.Core.Telemetry;

namespace VoiceCraft.Server;

public sealed class ServerTelemetryService(ServerProperties properties)
{
    private static readonly Stopwatch Uptime = Stopwatch.StartNew();
    public static TimeSpan HeartbeatInterval { get; } = TimeSpan.FromMinutes(1);
    private bool IsEnabled => properties.TelemetryEnabled;
    private string TelemetryToken => properties.TelemetryToken;

    public Task ReportStartupAsync(ServerTelemetrySnapshot snapshot)
    {
        return ReportTelemetryAsync(snapshot, "startup");
    }

    public Task ReportHeartbeatAsync(ServerTelemetrySnapshot snapshot)
    {
        return ReportTelemetryAsync(snapshot, "heartbeat");
    }

    public async Task<TelemetryDumpResponse?> ReportCrashAsync(Exception exception)
    {
        if (!IsEnabled)
            return null;

        var payload = new TelemetryDumpRequest
        {
            Role = "server",
            Category = "crash",
            Title = exception.GetType().Name,
            App = BuildAppInfo(),
            Device = BuildDeviceInfo(),
            Server = BuildServerInfo(),
            Payload = new Dictionary<string, string>
            {
                ["crash_log"] = exception.ToString(),
                ["uptime_sec"] = ((long)Uptime.Elapsed.TotalSeconds).ToString(CultureInfo.InvariantCulture)
            }
        };

        try
        {
            return await TelemetryTransport.SendDumpAsync(payload);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            return null;
        }
    }

    private async Task ReportTelemetryAsync(ServerTelemetrySnapshot snapshot, string tag)
    {
        if (!IsEnabled)
            return;

        var payload = new TelemetryEventRequest
        {
            Fingerprint = GetFingerprint(),
            Role = "server",
            App = BuildAppInfo(snapshot.Version),
            Device = BuildDeviceInfo(),
            Server = BuildServerInfo(snapshot),
            Metrics = new Dictionary<string, string>
            {
                ["positioning_type"] = snapshot.PositioningType,
                ["mc_http_enabled"] = snapshot.McHttpEnabled.ToString(),
                ["mc_tcp_enabled"] = snapshot.McTcpEnabled.ToString(),
                ["mc_wss_enabled"] = snapshot.McWssEnabled.ToString()
            },
            Tags = [tag],
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        try
        {
            await TelemetryTransport.SendTelemetryAsync(payload);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private string GetFingerprint()
    {
        return string.IsNullOrWhiteSpace(TelemetryToken)
            ? Guid.NewGuid().ToString("N")
            : TelemetryToken;
    }

    private static TelemetryAppInfo BuildAppInfo(string? version = null)
    {
        return new TelemetryAppInfo
        {
            AppName = "VoiceCraft",
            Version = version ?? ResolveVersion(),
            Channel = ResolveChannel(),
            Build = ResolveBuild()
        };
    }

    private static TelemetryDeviceInfo BuildDeviceInfo()
    {
        var totalAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long? memoryMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null;

        return new TelemetryDeviceInfo
        {
            OsName = GetPlatformName(),
            OsVersion = Environment.OSVersion.VersionString,
            OsBuild = Environment.OSVersion.Version.ToString(),
            OsDescription = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            Runtime = RuntimeInformation.FrameworkDescription,
            Locale = CultureInfo.CurrentUICulture.Name,
            CpuCores = Environment.ProcessorCount,
            MemoryMb = memoryMb
        };
    }

    private static TelemetryServerInfo BuildServerInfo(ServerTelemetrySnapshot? snapshot)
    {
        var totalAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long? memoryMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null;

        return new TelemetryServerInfo
        {
            Platform = GetPlatformName(),
            Architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            Locale = snapshot?.Language,
            CpuCores = Environment.ProcessorCount,
            MemoryMb = memoryMb,
            UptimeSec = (long)Uptime.Elapsed.TotalSeconds,
            ConnectedClients = snapshot?.ConnectedClients
        };
    }

    private static TelemetryServerInfo BuildServerInfo()
    {
        var totalAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

        return new TelemetryServerInfo
        {
            Platform = GetPlatformName(),
            Architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            CpuCores = Environment.ProcessorCount,
            MemoryMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null,
            UptimeSec = (long)Uptime.Elapsed.TotalSeconds
        };
    }

    private static string ResolveVersion()
    {
        return $"{Constants.Major}.{Constants.Minor}.{Constants.Patch}";
    }

    private static string ResolveBuild()
    {
        var informationalVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return informationalVersion ?? string.Empty;
    }

    private static string ResolveChannel()
    {
#if DEBUG
        return "debug";
#else
        return "stable";
#endif
    }

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows())
            return "Windows";
        if (OperatingSystem.IsLinux())
            return "Linux";
        return OperatingSystem.IsMacOS()
            ? "macOS"
            : RuntimeInformation.OSDescription;
    }
}

public sealed class ServerTelemetrySnapshot
{
    public string Version { get; init; } = string.Empty;
    public string Language { get; init; } = Constants.DefaultLanguage;
    public string PositioningType { get; init; } = string.Empty;
    public bool EnableVisibilityDisplay { get; init; }
    public bool McHttpEnabled { get; init; }
    public bool McTcpEnabled { get; init; }
    public bool McWssEnabled { get; init; }
    public int ConnectedClients { get; init; }
}