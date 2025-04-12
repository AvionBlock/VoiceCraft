using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services
{
    public class SettingsService
    {
        // ReSharper disable once InconsistentNaming
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        public AudioSettings AudioSettings => _settings.AudioSettings;
        public LocaleSettings LocaleSettings => _settings.LocaleSettings;
        public NotificationSettings NotificationSettings => _settings.NotificationSettings;
        public ServersSettings ServersSettings => _settings.ServersSettings;
        public ThemeSettings ThemeSettings => _settings.ThemeSettings;
        
        private bool _writing;
        private bool _queueWrite;
        private readonly SettingsStructure _settings = new();

        public SettingsService(NotificationService notificationService)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return;
                }

                var result = File.ReadAllText(SettingsPath);
                var loadedSettings = JsonSerializer.Deserialize<SettingsStructure>(result);
                if (loadedSettings == null)
                {
                    notificationService.SendErrorNotification("Failed to load settings file, Reverting to default.");
                    return;
                }
                
                loadedSettings.AudioSettings.OnLoading();
                loadedSettings.LocaleSettings.OnLoading();
                loadedSettings.NotificationSettings.OnLoading();
                loadedSettings.ServersSettings.OnLoading();
                loadedSettings.ThemeSettings.OnLoading();

                _settings = loadedSettings;
            }
            catch
            {
                notificationService.SendErrorNotification("Failed to load settings file, Reverting to default.");
            }
            
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
            
            await File.WriteAllTextAsync(SettingsPath,
                JsonSerializer.Serialize(_settings));
        }

        // ReSharper disable PropertyCanBeMadeInitOnly.Local
        private class SettingsStructure
        {
            public AudioSettings AudioSettings { get; set; } = new();
            public LocaleSettings LocaleSettings { get; set; } = new();
            public NotificationSettings NotificationSettings { get; set; } = new();
            public ServersSettings ServersSettings { get; set; } = new();
            public ThemeSettings ThemeSettings { get; set; } = new();
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
}