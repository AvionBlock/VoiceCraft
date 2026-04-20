using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using VoiceCraft.Core;
using VoiceCraft.Core.Telemetry;

namespace VoiceCraft.Server;

public static class ServerTelemetry
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(1);
    private static readonly object Sync = new();
    private static readonly Stopwatch Uptime = Stopwatch.StartNew();
    private static string? _telemetryToken;

    public static void SetTelemetryToken(string telemetryToken)
    {
        lock (Sync)
        {
            _telemetryToken = telemetryToken;
        }
    }

    public static Task ReportStartupAsync(ServerTelemetrySnapshot snapshot)
    {
        return ReportTelemetryAsync(snapshot, "startup");
    }

    public static Task ReportHeartbeatAsync(ServerTelemetrySnapshot snapshot)
    {
        return ReportTelemetryAsync(snapshot, "heartbeat");
    }

    public static TimeSpan GetHeartbeatInterval()
    {
        return HeartbeatInterval;
    }

    private static Task ReportTelemetryAsync(ServerTelemetrySnapshot snapshot, string tag)
    {
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

        return TelemetryTransport.SendTelemetryAsync(payload);
    }

    public static Task<TelemetryDumpResponse?> ReportCrashAsync(Exception exception)
    {
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

        return TelemetryTransport.SendDumpAsync(payload);
    }

    private static string GetFingerprint()
    {
        lock (Sync)
        {
            return string.IsNullOrWhiteSpace(_telemetryToken)
                ? Guid.NewGuid().ToString("N")
                : _telemetryToken;
        }
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
        return new TelemetryServerInfo
        {
            Platform = GetPlatformName(),
            Architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            CpuCores = Environment.ProcessorCount,
            MemoryMb = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes > 0
                ? GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024)
                : null,
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
        if (OperatingSystem.IsMacOS())
            return "macOS";

        return RuntimeInformation.OSDescription;
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
