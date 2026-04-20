using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Telemetry;

namespace VoiceCraft.Client.Services;

public static class ClientTelemetry
{
    private const int MaxCrashLogLength = 250_000;
    private static readonly object Sync = new();
    private static string? _telemetryToken;

    public static void SetTelemetryToken(string telemetryToken)
    {
        lock (Sync)
        {
            _telemetryToken = telemetryToken;
        }
    }

    public static Task ReportStartupAsync(SettingsService settingsService)
    {
        return ReportStartupAsync(settingsService, 1);
    }

    public static async Task ReportStartupAsync(SettingsService settingsService, int attempts)
    {
        var payload = new TelemetryEventRequest
        {
            Fingerprint = GetTelemetryToken(),
            Role = "client",
            App = BuildAppInfo(),
            Device = BuildDeviceInfo(),
            Metrics = new Dictionary<string, string>
            {
                ["positioning_type"] = settingsService.NetworkSettings.PositioningType.ToString(),
                ["push_to_talk_enabled"] = settingsService.InputSettings.PushToTalkEnabled.ToString()
            },
            Tags = ["startup"],
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        var retries = Math.Max(1, attempts);
        for (var attempt = 0; attempt < retries; attempt++)
        {
            if (await TelemetryTransport.SendTelemetryAsync(payload))
                return;

            if (attempt < retries - 1)
                await Task.Delay(1500);
        }
    }

    public static Task<TelemetryDumpResponse?> ReportCrashAsync(Exception exception)
    {
        return ReportCrashAsync(exception.ToString(), exception.GetType().Name);
    }

    public static Task<TelemetryDumpResponse?> ReportCrashAsync(string crashText, string? title = null)
    {
        var trimmedCrashText = TrimCrashText(crashText, out var wasTrimmed);
        var payload = new TelemetryDumpRequest
        {
            Role = "client",
            Category = "crash",
            Title = string.IsNullOrWhiteSpace(title) ? "CrashLog" : title,
            App = BuildAppInfo(),
            Device = BuildDeviceInfo(),
            Payload = new Dictionary<string, string>
            {
                ["crash_log"] = trimmedCrashText,
                ["truncated"] = wasTrimmed.ToString()
            }
        };

        return TelemetryTransport.SendDumpAsync(payload);
    }

    private static TelemetryAppInfo BuildAppInfo()
    {
        return new TelemetryAppInfo
        {
            AppName = "VoiceCraft",
            Version = ResolveVersion(),
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

    private static string GetTelemetryToken()
    {
        lock (Sync)
        {
            return string.IsNullOrWhiteSpace(_telemetryToken)
                ? Guid.NewGuid().ToString("N")
                : _telemetryToken;
        }
    }

    private static string ResolveVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
            return $"{version.Major}.{version.Minor}.{version.Build}";

        return $"{Constants.Major}.{Constants.Minor}.{Constants.Patch}";
    }

    private static string ResolveBuild()
    {
        var informationalVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
            return string.Empty;

        return informationalVersion;
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
        if (OperatingSystem.IsAndroid())
            return "Android";
        if (OperatingSystem.IsIOS())
            return "iOS";
        if (OperatingSystem.IsBrowser())
            return "Browser";

        return RuntimeInformation.OSDescription;
    }

    private static string TrimCrashText(string crashText, out bool wasTrimmed)
    {
        if (crashText.Length <= MaxCrashLogLength)
        {
            wasTrimmed = false;
            return crashText;
        }

        wasTrimmed = true;
        return crashText[..MaxCrashLogLength];
    }
}
