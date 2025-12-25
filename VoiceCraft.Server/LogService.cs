using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoiceCraft.Core;

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

    public static IEnumerable<KeyValuePair<DateTime, string>> CrashLogs =>
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
        _exceptionLogs.CrashLogs.TryAdd(DateTime.UtcNow, exception.ToString());
        TrimCrashLogs();
        SaveLogs();
    }

    public static void Load()
    {
        try
        {
            var fileDirectory = Path.Join(ConfigPath, FileName);
            if (!File.Exists(fileDirectory))
                return;

            var result = File.ReadAllText(fileDirectory);
            var loadedLogs =
                JsonSerializer.Deserialize<ExceptionLogsStructure>(result,
                    CrashLogGenerationContext.Default.ExceptionLogsStructure);
            if (loadedLogs == null) return;
            _exceptionLogs = loadedLogs;
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
            if (_exceptionLogs.CrashLogs.Count <= Limit) return;
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
    public ConcurrentDictionary<DateTime, string> CrashLogs { get; set; } = new();
    public ConcurrentDictionary<DateTime, string> ExceptionLogs { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ExceptionLogsStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class CrashLogGenerationContext : JsonSerializerContext;