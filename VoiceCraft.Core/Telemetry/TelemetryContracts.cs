using System.Collections.Generic;

namespace VoiceCraft.Core.Telemetry;

public class TelemetryAppInfo
{
    public string AppName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? Build { get; set; }
    public Dictionary<string, string>? Config { get; set; }
}

public class TelemetryDeviceInfo
{
    public string? OsVersion { get; set; }
    public string? OsName { get; set; }
    public string? OsBuild { get; set; }
    public string? OsDescription { get; set; }
    public string? Vendor { get; set; }
    public string? Model { get; set; }
    public string? Architecture { get; set; }
    public string? ProcessArchitecture { get; set; }
    public string? Runtime { get; set; }
    public string? Locale { get; set; }
    public int? CpuCores { get; set; }
    public long? MemoryMb { get; set; }
}

public class TelemetryServerInfo
{
    public int? CpuCores { get; set; }
    public long? MemoryMb { get; set; }
    public long? UptimeSec { get; set; }
    public string? Platform { get; set; }
    public string? Architecture { get; set; }
    public string? Locale { get; set; }
    public int? ConnectedClients { get; set; }
}

public class TelemetryEventRequest
{
    public string? Fingerprint { get; set; }
    public string Role { get; set; } = string.Empty;
    public TelemetryAppInfo App { get; set; } = new();
    public TelemetryDeviceInfo? Device { get; set; }
    public TelemetryServerInfo? Server { get; set; }
    public Dictionary<string, string>? Metrics { get; set; }
    public string[]? Tags { get; set; }
    public string? Timestamp { get; set; }
}

public class TelemetryDumpRequest
{
    public string? Fingerprint { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Title { get; set; }
    public TelemetryAppInfo? App { get; set; }
    public TelemetryDeviceInfo? Device { get; set; }
    public TelemetryServerInfo? Server { get; set; }
    public Dictionary<string, string> Payload { get; set; } = new();
}

public class TelemetryDumpResponse
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ViewUrl { get; set; }
    public string? JsonUrl { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}
