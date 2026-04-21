using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Diagnostics;

namespace VoiceCraft.Client.Services;

public static class LogService
{
    private const int Limit = 50;
    public static StorageService? NativeStorageService;
    private static ExceptionLogsStructure _exceptionLogs = new();
    private static bool _queueWrite;
    private static bool _writing;

    public static IEnumerable<KeyValuePair<DateTime, string>> ExceptionLogs =>
        _exceptionLogs.ExceptionLogs.OrderByDescending(d => d.Key);

    public static IEnumerable<KeyValuePair<DateTime, CrashLogRecord>> CrashLogs =>
        _exceptionLogs.CrashLogs.OrderByDescending(d => d.Key);

    public static void Load()
    {
        try
        {
            if (!NativeStorageService?.Exists(Constants.ExceptionLogsFile) ?? false)
                return;

            var result = NativeStorageService?.Load(Constants.ExceptionLogsFile);
            if (result == null)
                return;

            if (!TryLoadCurrent(result) && !TryLoadLegacy(result)) return;
            TrimExceptionLogs();
            TrimCrashLogs();
        }
        catch (JsonException ex)
        {
            Log(ex); //Log it, Don't care what we log.
        }
    }
    
    public static void LogCrash(Exception exception)
    {
        if (exception is TaskCanceledException) return; //Ignore this shit.
        Console.WriteLine(exception);
        _exceptionLogs.CrashLogs.TryAdd(DateTime.UtcNow, new CrashLogRecord
        {
            Message = exception.ToString()
        });
        TrimCrashLogs();
        SaveLogs();
    }
    
    public static void Log(Exception exception)
    {
        Console.WriteLine(exception);
        _exceptionLogs.ExceptionLogs.TryAdd(DateTime.UtcNow, exception.ToString());
        TrimExceptionLogs();
        _ = SaveAsync();
    }
    
    public static bool TryGetLog(DateTime timeStamp, [NotNullWhen(true)] out string? log)
    {
        return _exceptionLogs.ExceptionLogs.TryGetValue(timeStamp, out log);
    }

    public static void UpdateLog(DateTime timeStamp, string log)
    {
        if (!_exceptionLogs.ExceptionLogs.ContainsKey(timeStamp)) return;
        _exceptionLogs.ExceptionLogs[timeStamp] = log;
        _ = SaveAsync();
    }

    public static void ClearExceptionLogs()
    {
        _exceptionLogs.ExceptionLogs.Clear();
        _ = SaveAsync();
    }

    public static bool TryGetCrashLog(DateTime timeStamp, [NotNullWhen(true)] out CrashLogRecord? crashLog)
    {
        return _exceptionLogs.CrashLogs.TryGetValue(timeStamp, out crashLog);
    }
    
    public static void UpdateCrashLog(DateTime timeStamp, CrashLogRecord crashLog)
    {
        if (!_exceptionLogs.CrashLogs.ContainsKey(timeStamp)) return;
        _exceptionLogs.CrashLogs[timeStamp] = crashLog;
        _ = SaveAsync();
    }

    public static void ClearCrashLogs()
    {
        _exceptionLogs.CrashLogs.Clear();
        _ = SaveAsync(); //Since we don't to a save immediate, we need to call the save.
    }
    
    private static void TrimCrashLogs()
    {
        foreach (var log in _exceptionLogs.CrashLogs.OrderBy(d => d.Key))
        {
            if (_exceptionLogs.CrashLogs.Count <= Limit) return;
            _exceptionLogs.CrashLogs.TryRemove(log.Key, out _);
        }
    }

    private static void TrimExceptionLogs()
    {
        foreach (var log in _exceptionLogs.ExceptionLogs.OrderBy(d => d.Key))
        {
            if (_exceptionLogs.CrashLogs.Count <= Limit) return;
            _exceptionLogs.ExceptionLogs.TryRemove(log.Key, out _);
        }
    }

    private static bool TryLoadCurrent(byte[] result)
    {
        var loadedLogs = JsonSerializer.Deserialize(
            result,
            CrashLogGenerationContext.Default.ExceptionLogsStructure);
        if (loadedLogs == null)
            return false;

        _exceptionLogs = loadedLogs;
        return true;
    }

    private static bool TryLoadLegacy(byte[] result)
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
        NativeStorageService?.Save(Constants.ExceptionLogsFile,
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
