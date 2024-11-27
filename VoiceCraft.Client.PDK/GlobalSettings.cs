using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace VoiceCraft.Client.PDK
{
    public static class GlobalSettings
    {
        public static int FILE_WRITING_DELAY = 2000;

        private static bool _writing = false;
        private static bool _queueWrite = false;
        private static string SettingsPath = $"{AppContext.BaseDirectory}/GlobalSettings.json";
        private static ConcurrentDictionary<string, Type> _registeredSettings = new ConcurrentDictionary<string, Type>();
        private static ConcurrentDictionary<string, object> _settings = new ConcurrentDictionary<string, object>();

        public static void Set<T>(string key, T value) where T : notnull
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(key, out var registeredSetting) && registeredSetting == settingType)
            {
                _settings.AddOrUpdate(key, value, (key, old) => old = value);
                return;
            }

            throw new Exception($"Could not find registered setting {key} of type {typeof(T)}.");
        }

        public static T Get<T>(string key) where T : notnull
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(key, out var registeredSetting) && registeredSetting == settingType)
            {
                var setting = _settings.GetOrAdd(key, Activator.CreateInstance(settingType)!);
                return (T)setting;
            }

            throw new Exception($"Could not find registered setting {key} of type {typeof(T)}.");
        }

        public static void RegisterSetting<T>(string key)
        {
            var settingType = typeof(T);
            _registeredSettings.AddOrUpdate(key, settingType, (key, old) => old = settingType);
        }

        public static void UnregisterSetting(string key)
        {
            _registeredSettings.TryRemove(key, out _);
        }

        public static async Task SaveImmediate()
        {
#if DEBUG
            Debug.WriteLine("Saving immediately. Only use this function if necessary!");
#endif
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public static async Task SaveAsync()
        {
            _queueWrite = true;
            //Writing boolean is so we don't get multiple loop instances.
            if (!_writing)
            {
                _writing = true;
                while (_queueWrite)
                {
                    _queueWrite = false;
                    await Task.Delay(FILE_WRITING_DELAY);
                    await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
                }
                _writing = false;
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) { return; }

                var result = File.ReadAllText(SettingsPath);
                var loadedSettings = JsonSerializer.Deserialize<ConcurrentDictionary<string, object>>(result);
                if (loadedSettings is null) { return; }

                //Convert them to the actual objects.
                foreach (var setting in loadedSettings)
                {
                    if (_registeredSettings.TryGetValue(setting.Key, out var registeredSetting) && setting.Value is JsonElement element)
                    {
                        var deserializedSetting = element.Deserialize(registeredSetting);
                        if (deserializedSetting == null)
                        {
                            loadedSettings.TryRemove(setting.Key, out _);
                            continue;
                        }
                        loadedSettings.TryUpdate(setting.Key, deserializedSetting, setting.Value);
                        continue;
                    }
                    loadedSettings.TryRemove(setting.Key, out _);
                }
                _settings = loadedSettings;
            }
            catch (JsonException)
            {
                //Do nothing.
            }
        }
    }
}
