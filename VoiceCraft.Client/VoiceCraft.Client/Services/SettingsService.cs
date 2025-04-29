using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services
{
    public class SettingsService(StorageService storageService)
    {
        // ReSharper disable once InconsistentNaming
        public AudioSettings AudioSettings => _settings.AudioSettings;
        public LocaleSettings LocaleSettings => _settings.LocaleSettings;
        public NotificationSettings NotificationSettings => _settings.NotificationSettings;
        public ServersSettings ServersSettings => _settings.ServersSettings;
        public ThemeSettings ThemeSettings => _settings.ThemeSettings;
        
        private bool _writing;
        private bool _queueWrite;
        private SettingsStructure _settings = new();

        public void Load()
        {
            if (!storageService.Exists(Constants.SettingsDirectory))
                throw new FileNotFoundException("Settings file not found, Reverting to default.");
            
            var result = storageService.Load(Constants.SettingsDirectory);
            var loadedSettings = JsonSerializer.Deserialize<SettingsStructure>(result, SettingsStructureGenerationContext.Default.SettingsStructure);
            if (loadedSettings == null)
                throw new Exception("Failed to load settings file, Reverting to default.");
            
            loadedSettings.AudioSettings.OnLoading();
            loadedSettings.LocaleSettings.OnLoading();
            loadedSettings.NotificationSettings.OnLoading();
            loadedSettings.ServersSettings.OnLoading();
            loadedSettings.ThemeSettings.OnLoading();
            
            _settings = loadedSettings;
        }

        public async Task SaveImmediate()
        {
#if DEBUG
            Debug.WriteLine("Saving immediately. Only use this function if necessary!");
#endif
            await SaveSettingsAsync();
        }

        public async Task SaveAsync()
        {
            _queueWrite = true;
            //Writing boolean is so we don't get multiple loop instances.
            if (_writing) return;
            
            _writing = true;
            while (_queueWrite)
            {
                _queueWrite = false;
                await Task.Delay(Constants.FileWritingDelay);
                await SaveSettingsAsync();
            }

            _writing = false;
        }

        private async Task SaveSettingsAsync()
        {
            AudioSettings.OnSaving();
            LocaleSettings.OnSaving();
            NotificationSettings.OnSaving();
            ServersSettings.OnSaving();
            ThemeSettings.OnSaving();
            
            await storageService.SaveAsync(Constants.SettingsDirectory,
                JsonSerializer.SerializeToUtf8Bytes(_settings, SettingsStructureGenerationContext.Default.SettingsStructure));
        }
    }

    public abstract class Setting<T> : ISetting where T : Setting<T>
    {
        public abstract event Action<T>? OnUpdated;
        public virtual bool OnLoading() => true;

        public virtual void OnSaving()
        {
        }

        public abstract object Clone();
    }

    public interface ISetting : ICloneable
    {
        bool OnLoading();

        void OnSaving();
    }
    
    public class SettingsStructure
    {
        public AudioSettings AudioSettings { get; set; } = new();
        public LocaleSettings LocaleSettings { get; set; } = new();
        public NotificationSettings NotificationSettings { get; set; } = new();
        public ServersSettings ServersSettings { get; set; } = new();
        public ThemeSettings ThemeSettings { get; set; } = new();
    }
    
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SettingsStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class SettingsStructureGenerationContext : JsonSerializerContext;
}