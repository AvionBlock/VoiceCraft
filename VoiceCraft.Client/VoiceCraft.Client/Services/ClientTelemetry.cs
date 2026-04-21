using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using VoiceCraft.Core;
using VoiceCraft.Core.Telemetry;

namespace VoiceCraft.Client.Services;

public sealed class ClientTelemetry(SettingsService settingsService)
{
    private const int MaxCrashLogLength = 250_000;

    public bool IsEnabled =>
        settingsService.TelemetrySettings.Enabled &&
        settingsService.TelemetrySettings.ConsentShown;

    public Task ReportStartupAsync()
    {
        return ReportStartupAsync(1);
    }

    public async Task ReportStartupAsync(int attempts)
    {
        if (!IsEnabled)
            return;

        var payload = new TelemetryEventRequest
        {
            Fingerprint = settingsService.TelemetryToken,
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

    public Task<TelemetryDumpResponse?> ReportCrashAsync(Exception exception)
    {
        return ReportCrashAsync(exception.ToString(), exception.GetType().Name);
    }

    public Task<TelemetryDumpResponse?> ReportCrashAsync(string crashText, string? title = null)
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
        var deviceInfo = DeviceInfo.Current;
        var totalAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long? memoryMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null;

        return new TelemetryDeviceInfo
        {
            OsName = GetPlatformName(deviceInfo),
            OsVersion = GetOsVersion(deviceInfo),
            OsBuild = GetOsBuild(deviceInfo),
            OsDescription = GetOsDescription(deviceInfo),
            Vendor = NormalizeValue(deviceInfo.Manufacturer),
            Model = NormalizeValue(deviceInfo.Model),
            Architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            Runtime = RuntimeInformation.FrameworkDescription,
            Locale = CultureInfo.CurrentUICulture.Name,
            CpuCores = Environment.ProcessorCount,
            MemoryMb = memoryMb
        };
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

        return string.IsNullOrWhiteSpace(informationalVersion) ? string.Empty : informationalVersion;
    }

    private static string ResolveChannel()
    {
#if DEBUG
        return "debug";
#else
        return "stable";
#endif
    }

    private static string GetPlatformName(IDeviceInfo deviceInfo)
    {
        if (deviceInfo.Platform == DevicePlatform.WinUI)
            return IsWindows11(deviceInfo.Version) ? "Windows 11" : "Windows";
        if (deviceInfo.Platform == DevicePlatform.Android)
            return "Android";
        if (deviceInfo.Platform == DevicePlatform.iOS)
            return "iOS";
        if (deviceInfo.Platform == DevicePlatform.macOS)
            return "macOS";
        if (OperatingSystem.IsLinux())
            return "Linux";
        if (OperatingSystem.IsBrowser())
            return "Browser";

        return RuntimeInformation.OSDescription;
    }

    private static string GetOsVersion(IDeviceInfo deviceInfo)
    {
        if (deviceInfo.Platform == DevicePlatform.WinUI)
            return GetPlatformName(deviceInfo);
        if (deviceInfo.Platform == DevicePlatform.Android)
            return $"Android {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo.Platform == DevicePlatform.iOS)
            return $"iOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo.Platform == DevicePlatform.macOS)
            return $"macOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";

        var version = NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version);
        return string.IsNullOrWhiteSpace(version) ? Environment.OSVersion.VersionString : version;
    }

    private static string GetOsBuild(IDeviceInfo deviceInfo)
    {
        return deviceInfo.Version != default
            ? deviceInfo.Version.ToString()
            : Environment.OSVersion.Version.ToString();
    }

    private static string GetOsDescription(IDeviceInfo deviceInfo)
    {
        if (deviceInfo.Platform == DevicePlatform.WinUI)
            return $"{GetPlatformName(deviceInfo)} build {GetOsBuild(deviceInfo)}";
        if (deviceInfo.Platform == DevicePlatform.Android)
            return $"Android {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)} (API level {deviceInfo.Version.Major})";
        if (deviceInfo.Platform == DevicePlatform.iOS)
            return $"iOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo.Platform == DevicePlatform.macOS)
            return $"macOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";

        return RuntimeInformation.OSDescription;
    }

    private static string NormalizeVersionString(string? versionString, Version version)
    {
        if (!string.IsNullOrWhiteSpace(versionString))
            return versionString;

        return version != default ? version.ToString() : string.Empty;
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsWindows11(Version version)
    {
        return version.Major >= 10 && version.Build >= 22000;
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
