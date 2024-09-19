using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceCraft.Core.Settings;

namespace VoiceCraft.Core.Services
{
    public class SettingsService
    {
        private static string SettingsPath = $"{AppContext.BaseDirectory}/Settings.json";
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>> _registeredSettings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>>();
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, object>> _settings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, object>>();

        public void Set<T>(Guid id, T value) where T : Setting<T>
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(id, out var registeredSettings) && registeredSettings.TryGetValue(settingType.Name, out var registeredSetting) && registeredSetting == settingType)
            {
                var setting = _settings.GetOrAdd(id, new ConcurrentDictionary<string, object>());
                setting.AddOrUpdate(settingType.Name, value, (key, old) => old = value);
                return;
            }

            throw new Exception($"Could not find registered setting {settingType.Name} of type {typeof(T)} in {id}.");
        }

        public T Get<T>(Guid id) where T : Setting<T>
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(id, out var registeredSettings) && registeredSettings.TryGetValue(settingType.Name, out var registeredSetting) && registeredSetting == settingType)
            {
                var setting = _settings.GetOrAdd(id, new ConcurrentDictionary<string, object>());
                return (T)setting.GetOrAdd(settingType.Name, Activator.CreateInstance<T>());
            }

            throw new Exception($"Could not find registered setting {settingType.Name} of type {typeof(T)} in {id}.");
        }

        public void RegisterSetting<T>(Guid id) where T : Setting<T>
        {
            var settingType = typeof(T);
            var registeredSetting = _registeredSettings.GetOrAdd(id, new ConcurrentDictionary<string, Type>());
            registeredSetting.AddOrUpdate(settingType.Name, settingType, (key, old) => old = settingType);
        }

        public void UnregisterSetting<T>(Guid id) where T : Setting<T>
        {
            if (_registeredSettings.TryGetValue(id, out var registeredSettings))
            {
                registeredSettings.TryRemove(typeof(T).Name, out _);
            }
        }

        public async Task SaveAsync()
        {
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) { return; }

                var result = File.ReadAllText(SettingsPath);
                var loadedSettings = JsonSerializer.Deserialize<ConcurrentDictionary<Guid, ConcurrentDictionary<string, object>>>(result);
                if (loadedSettings is null) { return; }

                //Convert them to the actual objects.
                foreach (var settings in loadedSettings)
                {
                    if (_registeredSettings.TryGetValue(settings.Key, out var registeredSettings))
                    {
                        foreach (var setting in settings.Value)
                        {
                            if (setting.Value is JsonElement element
                                && registeredSettings.TryGetValue(setting.Key, out var registeredSetting)
                                && element.Deserialize(registeredSetting) is object deserializedSetting)
                            {
                                _ = settings.Value.TryUpdate(setting.Key, deserializedSetting, setting.Value);
                                continue;
                            }
                            settings.Value.TryRemove(setting.Key, out _);
                        }
                        continue;
                    }
                    loadedSettings.TryRemove(settings.Key, out _);
                }
                _settings = loadedSettings;
            }
            catch(JsonException)
            {
                //Do nothing.
            }
        }
    }
}
