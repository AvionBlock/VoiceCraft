using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services;

public static class LogService
{
    public static StorageService? NativeStorageService;
    private const int Limit = 50;
    private static ExceptionLogsStructure _exceptionLogs = new();
    private static bool _queueWrite;
    private static bool _writing;
    public static IEnumerable<KeyValuePair<DateTime, string>> ExceptionLogs => _exceptionLogs.ExceptionLogs.OrderByDescending(d => d.Key);
    public static IEnumerable<KeyValuePair<DateTime, string>> CrashLogs => _exceptionLogs.CrashLogs.OrderByDescending(d => d.Key);

    public static void Log(Exception exception)
    {
        _exceptionLogs.ExceptionLogs.TryAdd(DateTime.UtcNow, exception.ToString());
        _ = SaveAsync();
    }
    
    public static void LogCrash(Exception exception)
    {
        _exceptionLogs.CrashLogs.TryAdd(DateTime.UtcNow, exception.ToString());
        SaveLogs();
    }

    public static void Load()
    {
        try
        {
            if (!NativeStorageService?.Exists(Constants.ExceptionLogsFile) ?? false)
                return;

            var result = NativeStorageService?.Load(Constants.ExceptionLogsFile);
            var loadedLogs =
                JsonSerializer.Deserialize<ExceptionLogsStructure>(result, CrashLogGenerationContext.Default.ExceptionLogsStructure);
            if (loadedLogs == null) return;

            var exceptionLogs = loadedLogs.ExceptionLogs.Count;
            if(exceptionLogs > Limit)
                loadedLogs.ExceptionLogs = new ConcurrentDictionary<DateTime, string>(loadedLogs.ExceptionLogs.Skip(exceptionLogs - Limit));
            
            var crashLogs = loadedLogs.CrashLogs.Count;
            if(crashLogs > Limit)
                loadedLogs.CrashLogs = new ConcurrentDictionary<DateTime, string>(loadedLogs.CrashLogs.Skip(crashLogs - Limit));

            _exceptionLogs = loadedLogs;
        }
        catch (JsonException)
        {
            //Do Nothing.
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
            JsonSerializer.SerializeToUtf8Bytes(_exceptionLogs, CrashLogGenerationContext.Default.ExceptionLogsStructure));
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