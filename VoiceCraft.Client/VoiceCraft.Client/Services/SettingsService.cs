using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

namespace VoiceCraft.Client.Services
{
    public class SettingsService
    {
        public const int FILE_WRITING_DELAY = 2000;

        private bool _writing;
        private bool _queueWrite;
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
        private readonly ConcurrentDictionary<string, Type> _registeredSettings = new();
        private ConcurrentDictionary<string, object?> _settings = new();
        
        public T Get<T>() where T : Setting<T>
        {
            var settingType = typeof(T);
            if (_registeredSettings.TryGetValue(settingType.Name, out var registeredSetting) && registeredSetting == settingType)
            {
                return (T)_settings.GetOrAdd(settingType.Name, Activator.CreateInstance<T>())!;
            }

            throw new Exception($"Could not find registered setting {settingType.Name} of type {typeof(T)}.");
        }

        public void RegisterSetting<T>() where T : Setting<T>
        {
            var settingType = typeof(T);
            _registeredSettings.AddOrUpdate(settingType.Name, settingType, (_, _) => settingType);
        }

        public void UnregisterSetting<T>() where T : Setting<T>
        {
            var settingType = typeof(T);
            _registeredSettings.TryRemove(settingType.Name, out _);
        }

        public async Task SaveImmediate()
        {
#if DEBUG
            Debug.WriteLine("Saving immediately. Only use this function if necessary!");
#endif
            foreach (var setting in _settings)
            {
                if (setting.Value is ISetting settingValue)
                    settingValue.OnSaving();
            }

            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, SerializerOptions));
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
                    foreach (var setting in _settings)
                    {
                        if (setting.Value is ISetting settingValue)
                            settingValue.OnSaving();
                    }

                    await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, SerializerOptions));
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
                var loadedSettings = JsonSerializer.Deserialize<ConcurrentDictionary<string, object?>>(result);
                if (loadedSettings is null) { return; }

                //Convert them to the actual objects.
                foreach (var setting in loadedSettings)
                {
                    if (_registeredSettings.TryGetValue(setting.Key, out var registeredSetting))
                    {
                        if (setting.Value is JsonElement element
                        && element.Deserialize(registeredSetting) is ISetting deserializedSetting
                        && deserializedSetting.OnLoading())
                        {
                            loadedSettings.TryUpdate(setting.Key, deserializedSetting, setting.Value);
                            continue;
                        }
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

    public abstract class Setting<T> : ISetting where T : Setting<T>
    {
        public abstract event Action<T>? OnUpdated;
        public virtual bool OnLoading() => true;
        public virtual void OnSaving() { }
        public abstract object Clone();
    }

    public interface ISetting : ICloneable
    {
        bool OnLoading();

        void OnSaving();
    }
}