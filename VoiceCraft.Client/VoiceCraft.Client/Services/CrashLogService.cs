using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace VoiceCraft.Client.Services
{
    public static class CrashLogService
    {
        private const int Limit = 50;
        private static readonly string CrashLogsPath = Path.Combine(AppContext.BaseDirectory, "CrashLogs.json");
        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
        private static Dictionary<DateTime, string> _crashLogs = new();
        public static IEnumerable<KeyValuePair<DateTime, string>> CrashLogs => _crashLogs;

        public static void Log(Exception exception)
        {
            _crashLogs.TryAdd(DateTime.UtcNow, exception.ToString());
            File.WriteAllText(CrashLogsPath, JsonSerializer.Serialize(_crashLogs, SerializerOptions));
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(CrashLogsPath))
                {
                    return;
                }

                var result = File.ReadAllText(CrashLogsPath);
                var loadedCrashLogs = JsonSerializer.Deserialize<Dictionary<DateTime, string>>(result);
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
            File.WriteAllText(CrashLogsPath, JsonSerializer.Serialize(_crashLogs, SerializerOptions));
        }
    }
}