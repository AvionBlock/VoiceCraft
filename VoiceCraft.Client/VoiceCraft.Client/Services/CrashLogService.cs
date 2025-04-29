using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services
{
    public static class CrashLogService
    {
        private const int Limit = 50;
        public static StorageService? NativeStorageService;

        private static Dictionary<DateTime, string> _crashLogs = new();
        public static IEnumerable<KeyValuePair<DateTime, string>> CrashLogs => _crashLogs;

        public static void Log(Exception exception)
        {
            _crashLogs.TryAdd(DateTime.UtcNow, exception.ToString());
            NativeStorageService?.Save(Constants.CrashLogsFile,
                JsonSerializer.SerializeToUtf8Bytes(_crashLogs, CrashLogGenerationContext.Default.DictionaryDateTimeString));
        }

        public static void Load()
        {
            try
            {
                if (!NativeStorageService?.Exists(Constants.CrashLogsFile) ?? false)
                    return;

                var result = NativeStorageService?.Load(Constants.CrashLogsFile);
                var loadedCrashLogs =
                    JsonSerializer.Deserialize<Dictionary<DateTime, string>>(result, CrashLogGenerationContext.Default.DictionaryDateTimeString);
                if (loadedCrashLogs == null) return;

                if (loadedCrashLogs.Count > Limit)
                {
                    while (loadedCrashLogs.Count > Limit)
                    {
                        loadedCrashLogs = loadedCrashLogs.Skip(loadedCrashLogs.Count - Limit).ToDictionary(x => x.Key, x => x.Value);
                    }
                }

                _crashLogs = loadedCrashLogs;
            }
            catch (JsonException)
            {
                //Do Nothing.
            }
        }

        public static void Clear()
        {
            _crashLogs.Clear();
            NativeStorageService?.Save(Constants.CrashLogsFile,
                JsonSerializer.SerializeToUtf8Bytes(_crashLogs, CrashLogGenerationContext.Default.DictionaryDateTimeString));
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Dictionary<DateTime, string>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class CrashLogGenerationContext : JsonSerializerContext;
}