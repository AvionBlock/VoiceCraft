using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class OutputSettingsDataViewModel : ObservableObject, IDisposable
{
    private readonly AudioService _audioService;
    private readonly OutputSettings _outputSettings;
    private readonly SettingsService _settingsService;
    
    [ObservableProperty] private string _outputDevice;
    [ObservableProperty] private float _outputVolume;
    [ObservableProperty] private Guid _audioClipper;
    private bool _disposed;
    private bool _updating;
    
    //Lists
    [ObservableProperty] private ObservableCollection<string> _outputDevices = [];
    [ObservableProperty] private ObservableCollection<RegisteredAudioClipper> _audioClippers = [];
    
    public OutputSettingsDataViewModel(SettingsService settingsService, AudioService audioService)
    {
        _outputSettings = settingsService.OutputSettings;
        _audioService = audioService;
        _settingsService = settingsService;

        _outputSettings.OnUpdated += Update;
        _outputDevice = _outputSettings.OutputDevice;
        _outputVolume = _outputSettings.OutputVolume;
        _audioClipper = _outputSettings.AudioClipper;
        
        _ = ReloadAvailableDevices();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _outputSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    public async Task ReloadAvailableDevices()
    {
        OutputDevices = ["Default", ..await _audioService.GetOutputDevicesAsync()];
        AudioClippers = new ObservableCollection<RegisteredAudioClipper>(_audioService.RegisteredAudioClippers);
        
        if (!OutputDevices.Contains(OutputDevice))
            OutputDevice = "Default";
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