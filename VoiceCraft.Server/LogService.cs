using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Core;
using VoiceCraft.Core.Diagnostics;

namespace VoiceCraft.Server;

public static class LogService
{
    private const string FileName = "CrashLogs.json";
    private const string ConfigPath = "config";
    private const int Limit = 100;
    private static ExceptionLogsStructure _exceptionLogs = new();
    private static bool _queueWrite;
    private static bool _writing;

    public static IEnumerable<KeyValuePair<DateTime, string>> ExceptionLogs =>
        _exceptionLogs.ExceptionLogs.OrderByDescending(d => d.Key);

    public static IEnumerable<KeyValuePair<DateTime, CrashLogRecord>> CrashLogs =>
        _exceptionLogs.CrashLogs.OrderByDescending(d => d.Key);

    public static void Log(Exception exception)
    {
        _exceptionLogs.ExceptionLogs.TryAdd(DateTime.UtcNow, exception.ToString());
        TrimExceptionLogs();
        _ = SaveAsync();
    }

    public static void LogCrash(Exception exception)
    {
        Console.WriteLine(exception);
        var timestamp = DateTime.UtcNow;
        _exceptionLogs.CrashLogs.TryAdd(timestamp, new CrashLogRecord
        {
            Message = exception.ToString()
        });
        TrimCrashLogs();
        SaveLogs();
        _ = AttachDumpUrlAsync(timestamp, exception);
    }

    public static void Load()
    {
        try
        {
            var fileDirectory = Path.Join(ConfigPath, FileName);
            if (!File.Exists(fileDirectory))
                return;

            var result = File.ReadAllText(fileDirectory);
            if (!TryLoadCurrent(result) && !TryLoadLegacy(result)) return;
            TrimExceptionLogs();
            TrimCrashLogs();
        }
        catch (JsonException ex)
        {
            Log(ex); //Log it, Don't care what we log.
        }
    }

    public static void ClearCrashLogs()
    {
        _exceptionLogs.CrashLogs.Clear();
        _ = SaveAsync(); //Since we don't to a save immediate, we need to call the save.
    }

    public static void ClearExceptionLogs()
    {
        _exceptionLogs.ExceptionLogs.Clear();
        _ = SaveAsync();
    }

    private static void TrimExceptionLogs()
    {
        foreach (var log in _exceptionLogs.ExceptionLogs.OrderBy(d => d.Key))
        {
            if (_exceptionLogs.ExceptionLogs.Count <= Limit) return;
            _exceptionLogs.ExceptionLogs.TryRemove(log.Key, out _);
        }
    }

    private static void TrimCrashLogs()
    {
        foreach (var log in _exceptionLogs.CrashLogs.OrderBy(d => d.Key))
        {
            if (_exceptionLogs.CrashLogs.Count <= Limit) return;
            _exceptionLogs.CrashLogs.TryRemove(log.Key, out _);
        }
    }

    private static bool TryLoadCurrent(string result)
    {
        var loadedLogs = JsonSerializer.Deserialize(
            result,
            CrashLogGenerationContext.Default.ExceptionLogsStructure);
        if (loadedLogs == null)
            return false;

        _exceptionLogs = loadedLogs;
        return true;
    }

    private static bool TryLoadLegacy(string result)
    {
        var loadedLogs = JsonSerializer.Deserialize(
            result,
            CrashLogGenerationContext.Default.LegacyExceptionLogsStructure);
        if (loadedLogs == null)
            return false;

        _exceptionLogs = new ExceptionLogsStructure
        {
            ExceptionLogs = loadedLogs.ExceptionLogs,
            CrashLogs = new ConcurrentDictionary<DateTime, CrashLogRecord>(
                loadedLogs.CrashLogs.ToDictionary(
                    x => x.Key,
                    x => new CrashLogRecord { Message = x.Value }))
        };
        return true;
    }

    private static async Task AttachDumpUrlAsync(DateTime timestamp, Exception exception)
    {
        var telemetry = Program.ServiceProvider.GetService<ServerTelemetryService>();
        if (telemetry == null)
            return;

        var dumpResponse = await telemetry.ReportCrashAsync(exception);
        var dumpUrl = dumpResponse?.ViewUrl ?? dumpResponse?.Url;
        if (string.IsNullOrWhiteSpace(dumpUrl))
            return;

        if (!_exceptionLogs.CrashLogs.TryGetValue(timestamp, out var crashLog))
            return;

        crashLog.DumpUrl = dumpUrl;
        SaveLogs();
    }

    private static async Task SaveAsync()
    {
        _queueWrite = true;
        //Writing boolean is so we don't get multiple loop instances.
        if (_writing) return;

        _writing = true;
        while (_queueWrite)
        {
            _queueWrite = false;
            await Task.Delay(Constants.FileWritingDelay);
            SaveLogs();
        }

        _writing = false;
    }

    private static void SaveLogs()
    {
        if (!Directory.Exists(ConfigPath))
            Directory.CreateDirectory(ConfigPath);

        File.WriteAllBytes(Path.Join(ConfigPath, FileName),
            JsonSerializer.SerializeToUtf8Bytes(_exceptionLogs,
                CrashLogGenerationContext.Default.ExceptionLogsStructure));
    }
}

public class ExceptionLogsStructure
{
    public ConcurrentDictionary<DateTime, CrashLogRecord> CrashLogs { get; set; } = new();
    public ConcurrentDictionary<DateTime, string> ExceptionLogs { get; set; } = new();
}

public class LegacyExceptionLogsStructure
{
    public ConcurrentDictionary<DateTime, string> CrashLogs { get; set; } = new();
    public ConcurrentDictionary<DateTime, string> ExceptionLogs { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ExceptionLogsStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(LegacyExceptionLogsStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class CrashLogGenerationContext : JsonSerializerContext;
