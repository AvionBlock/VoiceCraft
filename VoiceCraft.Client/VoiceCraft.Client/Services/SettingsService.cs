using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services;

public class SettingsService(StorageService storageService)
{
    private bool _queueWrite;
    private SettingsStructure _settings = new();

    private bool _writing;

    // ReSharper disable once InconsistentNaming
    public Guid UserGuid => _settings.UserGuid;
    public Guid ServerUserGuid => _settings.ServerUserGuid;
    public AudioSettings AudioSettings => _settings.AudioSettings;
    public LocaleSettings LocaleSettings => _settings.LocaleSettings;
    public NotificationSettings NotificationSettings => _settings.NotificationSettings;
    public ServersSettings ServersSettings => _settings.ServersSettings;
    public ThemeSettings ThemeSettings => _settings.ThemeSettings;
    public NetworkSettings NetworkSettings => _settings.NetworkSettings;
    public UserSettings UserSettings => _settings.UserSettings;

    public void Load()
    {
        if (!storageService.Exists(Constants.SettingsFile))
            throw new FileNotFoundException("Settings file not found, Reverting to default.");

        var result = storageService.Load(Constants.SettingsFile);
        var loadedSettings = JsonSerializer.Deserialize<SettingsStructure>(result, SettingsStructureGenerationContext.Default.SettingsStructure);
        if (loadedSettings == null)
            throw new Exception("Failed to load settings file, Reverting to default.");

        loadedSettings.AudioSettings.OnLoading();
        loadedSettings.LocaleSettings.OnLoading();
        loadedSettings.NotificationSettings.OnLoading();
        loadedSettings.ServersSettings.OnLoading();
        loadedSettings.ThemeSettings.OnLoading();
        loadedSettings.NetworkSettings.OnLoading();
        loadedSettings.UserSettings.OnLoading();

        _settings = loadedSettings;
    }

    public async Task SaveImmediate()
    {
        Debug.WriteLine("Saving immediately. Only use this function if necessary!");
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
        NetworkSettings.OnSaving();
        UserSettings.OnSaving();

        await storageService.SaveAsync(Constants.SettingsFile,
            JsonSerializer.SerializeToUtf8Bytes(_settings, SettingsStructureGenerationContext.Default.SettingsStructure));
    }
}

public abstract class Setting<T> : ISetting where T : Setting<T>
{
    public virtual bool OnLoading()
    {
        return true;
    }

    public virtual void OnSaving()
    {
    }

    public abstract object Clone();
    public abstract event Action<T>? OnUpdated;
}

public interface ISetting : ICloneable
{
    bool OnLoading();

    void OnSaving();
}

public class SettingsStructure
{
    public Guid UserGuid { get; set; } = Guid.NewGuid();
    public Guid ServerUserGuid { get; set; } = Guid.NewGuid();
    public AudioSettings AudioSettings { get; set; } = new();
    public LocaleSettings LocaleSettings { get; set; } = new();
    public NotificationSettings NotificationSettings { get; set; } = new();
    public ServersSettings ServersSettings { get; set; } = new();
    public ThemeSettings ThemeSettings { get; set; } = new();
    public NetworkSettings NetworkSettings { get; set; } = new();
    public UserSettings UserSettings { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SettingsStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SettingsStructureGenerationContext : JsonSerializerContext;