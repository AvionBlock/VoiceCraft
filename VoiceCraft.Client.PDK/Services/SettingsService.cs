using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Concurrent;
using System.Text.Json;

namespace VoiceCraft.Client.PDK.Services
{
    public class SettingsService
    {
        private const int FILE_WRITING_DELAY = 2000;

        private bool _writing = false;
        private bool _queueWrite = false;
        private static string SettingsPath = $"{AppContext.BaseDirectory}/Settings.json";
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>> _registeredSettings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, Type>>();
        private ConcurrentDictionary<Guid, ConcurrentDictionary<string, object>> _settings = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, object>>();

        public void Set<T>(Guid id, T value) where T : Setting
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

        public T Get<T>(Guid id) where T : Setting
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(id, out var registeredSettings) && registeredSettings.TryGetValue(settingType.Name, out var registeredSetting) && registeredSetting == settingType)
            {
                var setting = _settings.GetOrAdd(id, new ConcurrentDictionary<string, object>());
                return (T)setting.GetOrAdd(settingType.Name, Activator.CreateInstance<T>());
            }

            throw new Exception($"Could not find registered setting {settingType.Name} of type {typeof(T)} in {id}.");
        }

        public void RegisterSetting<T>(Guid id) where T : Setting
        {
            var settingType = typeof(T);
            var registeredSetting = _registeredSettings.GetOrAdd(id, new ConcurrentDictionary<string, Type>());
            registeredSetting.AddOrUpdate(settingType.Name, settingType, (key, old) => old = settingType);
        }

        public void UnregisterSetting<T>(Guid id) where T : Setting
        {
            if (_registeredSettings.TryGetValue(id, out var registeredSettings))
            {
                registeredSettings.TryRemove(typeof(T).Name, out _);
            }
        }

        public async Task SaveAsync()
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
                    foreach (var settings in _settings)
                    {
                        foreach (var setting in settings.Value)
                        {
                            if (setting.Value is Setting settingValue)
                                settingValue.OnSaving();
                        }
                    }

                    await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
                }
                _writing = false;
            }
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
                                && element.Deserialize(registeredSetting) is Setting deserializedSetting
                                && deserializedSetting.OnLoading())
                            {
                                settings.Value.TryUpdate(setting.Key, deserializedSetting, setting.Value);
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
            catch (JsonException)
            {
                //Do nothing.
            }
        }
    }

    public abstract class Setting : ObservableObject, ICloneable
    {
        public virtual bool OnLoading() => true;

        public virtual void OnSaving() { }

        public abstract object Clone();
    }
}
