using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class OutputSettingsDataViewModel : ObservableObject, IDisposable
{
    private readonly AudioService _audioService;
    private readonly OutputSettings _outputSettings;
    private readonly SettingsService _settingsService;

    [ObservableProperty] public partial string OutputDevice { get; set; }
    [ObservableProperty] public partial float OutputVolume { get; set; }
    [ObservableProperty] public partial Guid AudioClipper { get; set; }

    private bool _disposed;
    private bool _updating;

    //Lists
    [ObservableProperty] public partial ObservableCollection<AudioDeviceInfo> OutputDevices { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<RegisteredAudioClipper> AudioClippers { get; set; } = [];

    public OutputSettingsDataViewModel(SettingsService settingsService, AudioService audioService)
    {
        _outputSettings = settingsService.OutputSettings;
        _audioService = audioService;
        _settingsService = settingsService;

        _outputSettings.OnUpdated += Update;
        OutputDevice = _outputSettings.OutputDevice;
        OutputVolume = _outputSettings.OutputVolume;
        AudioClipper = _outputSettings.AudioClipper;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _outputSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void ReloadDevices()
    {
        OutputDevices = [.._audioService.GetOutputDevices()];
        AudioClippers = [.._audioService.RegisteredAudioClippers];
        if (OutputDevices.All(outputDevice => outputDevice.Name != OutputDevice))
            OutputDevice = OutputDevices.First(x => x.IsDefault).Name;
        if (AudioClippers.FirstOrDefault(x => x.Id == AudioClipper) == null)
            AudioClipper = Guid.Empty;
    }

    partial void OnOutputDeviceChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _outputSettings.OutputDevice = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnOutputVolumeChanging(float value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _outputSettings.OutputVolume = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnAudioClipperChanging(Guid value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _outputSettings.AudioClipper = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(OutputSettings outputSettings)
    {
        if (_updating) return;
        _updating = true;

        OutputDevice = outputSettings.OutputDevice;
        OutputVolume = outputSettings.OutputVolume;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(InputSettingsDataViewModel).ToString());
    }
}