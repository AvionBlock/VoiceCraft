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

public sealed class ClientTelemetryService(SettingsService settingsService)
{
    private const int MaxCrashLogLength = 250_000;
    private readonly TelemetryTransport _transport = new();
    private bool IsEnabled =>
        settingsService.TelemetrySettings is { Enabled: true, ConsentShown: true };

    public async Task ReportStartupAsync()
    {
        await ReportStartupAsync(3);
    }

    public async Task<TelemetryDumpResponse?> ReportCrashAsync(string crashText, string? title = null)
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

        try
        {
            return await _transport.SendDumpAsync(payload);
        }
        catch(Exception ex)
        {
            LogService.Log(ex);
            return null;
        }
    }

    private async Task ReportStartupAsync(int attempts)
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
            try
            {
                await _transport.SendTelemetryAsync(payload);
            }
            catch(Exception ex)
            {
                LogService.Log(ex);
                if (attempt < retries - 1)
                    await Task.Delay(1500);
            }
        }
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
        var deviceInfo = TryGetDeviceInfo();
        var totalAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        long? memoryMb = totalAvailableBytes > 0 ? totalAvailableBytes / (1024 * 1024) : null;

        return new TelemetryDeviceInfo
        {
            OsName = GetPlatformName(deviceInfo),
            OsVersion = GetOsVersion(deviceInfo),
            OsBuild = GetOsBuild(deviceInfo),
            OsDescription = GetOsDescription(deviceInfo),
            Vendor = NormalizeValue(deviceInfo?.Manufacturer),
            Model = NormalizeValue(deviceInfo?.Model),
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
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : $"{Constants.Major}.{Constants.Minor}.{Constants.Patch}";
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

    private static IDeviceInfo? TryGetDeviceInfo()
    {
        if (!(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacOS()))
            return null;

        try
        {
            return DeviceInfo.Current;
        }
        catch
        {
            return null;
        }
    }

    private static string GetPlatformName(IDeviceInfo? deviceInfo)
    {
        if (deviceInfo?.Platform == DevicePlatform.WinUI)
            return IsWindows11(deviceInfo.Version) ? "Windows 11" : "Windows";
        if (deviceInfo?.Platform == DevicePlatform.Android)
            return "Android";
        if (deviceInfo?.Platform == DevicePlatform.iOS)
            return "iOS";
        if (deviceInfo?.Platform == DevicePlatform.macOS)
            return "macOS";
        if (OperatingSystem.IsWindows())
            return IsWindows11(Environment.OSVersion.Version) ? "Windows 11" : "Windows";
        if (OperatingSystem.IsLinux())
            return "Linux";
        return OperatingSystem.IsBrowser()
            ? "Browser"
            : RuntimeInformation.OSDescription;
    }

    private static string GetOsVersion(IDeviceInfo? deviceInfo)
    {
        if (deviceInfo?.Platform == DevicePlatform.WinUI)
            return GetPlatformName(deviceInfo);
        if (deviceInfo?.Platform == DevicePlatform.Android)
            return $"Android {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo?.Platform == DevicePlatform.iOS)
            return $"iOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo?.Platform == DevicePlatform.macOS)
            return $"macOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (OperatingSystem.IsWindows())
            return GetPlatformName(deviceInfo);

        if (deviceInfo == null) return Environment.OSVersion.VersionString;
        var version = NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version);
        return !string.IsNullOrWhiteSpace(version)
            ? version
            : Environment.OSVersion.VersionString;
    }

    private static string GetOsBuild(IDeviceInfo? deviceInfo)
    {
        return deviceInfo is not null
            ? deviceInfo.Version.ToString()
            : Environment.OSVersion.Version.ToString();
    }

    private static string GetOsDescription(IDeviceInfo? deviceInfo)
    {
        if (deviceInfo?.Platform == DevicePlatform.WinUI)
            return $"{GetPlatformName(deviceInfo)} build {GetOsBuild(deviceInfo)}";
        if (deviceInfo?.Platform == DevicePlatform.Android)
            return
                $"Android {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)} (API level {deviceInfo.Version.Major})";
        if (deviceInfo?.Platform == DevicePlatform.iOS)
            return $"iOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        if (deviceInfo?.Platform == DevicePlatform.macOS)
            return $"macOS {NormalizeVersionString(deviceInfo.VersionString, deviceInfo.Version)}";
        return OperatingSystem.IsWindows()
            ? $"{GetPlatformName(deviceInfo)} build {Environment.OSVersion.Version}"
            : RuntimeInformation.OSDescription;
    }

    private static string NormalizeVersionString(string? versionString, Version version)
    {
        return !string.IsNullOrWhiteSpace(versionString)
            ? versionString
            : version.ToString();
    }

    private static string? NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsWindows11(Version version)
    {
        return version is { Major: >= 10, Build: >= 22000 };
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