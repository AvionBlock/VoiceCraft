using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Services
{
    public class SettingsService
    {
        private static string SettingsPath = $"{AppContext.BaseDirectory}/Settings.json";
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>> _registeredSettings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>>();
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, dynamic>> _settings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, dynamic>>();

        public void Set<T>(Guid id, string settingId, T value) where T : unmanaged
        {
            if (_registeredSettings.TryGetValue(id, out var registeredSettings) && registeredSettings.TryGetValue(settingId, out var rSetting) && rSetting == typeof(ISetting<T>))
            {
                _settings.TryGetValue(id, out var settings);
                settings.AddOrUpdate(settingId, value, (key, old) => old = value);
            }

            throw new Exception($"Could not find registered setting {settingId} of type {typeof(T)} in {id}.");
        }

        public T Get<T>(Guid id, string settingId) where T : unmanaged
        {
            if (_registeredSettings.TryGetValue(id, out var registeredSettings) && registeredSettings.TryGetValue(settingId, out var rSetting) && rSetting is ISetting<T> registeredSetting)
            {
                if(_settings.TryGetValue(id, out var settings) && settings.TryGetValue(settingId, out var settingValue) && settingValue is T setting)
                {
                    return setting;
                }
                return registeredSetting.Default;
            }
            throw new Exception($"Could not find registered setting {settingId} of type {typeof(T)} in {id}.");
        }

        public void RegisterSetting<T>(Guid id, ISetting<T> setting) where T : unmanaged
        {
            var registeredSetting = _registeredSettings.GetOrAdd(id, new ConcurrentDictionary<string, Type>());
            registeredSetting.AddOrUpdate(setting.Id, setting.GetType(), (key, old) => old = setting.GetType());
        }

        public void UnregisterSetting(Guid id, string settingId)
        {
            if(_registeredSettings.TryGetValue(id, out var registeredSettings))
            {
                registeredSettings.TryRemove(settingId, out _);
            }
        }

        public async Task SaveAsync()
        {
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings));
        }

        public async Task LoadAsync()
        {
            if (File.Exists(SettingsPath))
            {
                var result = await File.ReadAllTextAsync(SettingsPath);
                var settings = JsonSerializer.Deserialize<ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>>>(result);
                if (settings == null) return;
            }
        }
    }
}
