using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class InputSettingsDataViewModel : ObservableObject, IDisposable
{
    private readonly AudioService _audioService;
    private readonly InputSettings _inputSettings;
    private readonly SettingsService _settingsService;

    [ObservableProperty] private string _inputDevice;
    [ObservableProperty] private float _inputVolume;
    [ObservableProperty] private float _microphoneSensitivity;
    [ObservableProperty] private Guid _denoiser;
    [ObservableProperty] private Guid _automaticGainController;
    [ObservableProperty] private Guid _echoCanceler;
    [ObservableProperty] private bool _pushToTalkEnabled;
    [ObservableProperty] private bool _pushToTalkCue;

    //Lists
    [ObservableProperty] private ObservableCollection<AudioDeviceInfo> _inputDevices = [];
    [ObservableProperty] private ObservableCollection<RegisteredAudioPreprocessor> _denoisers = [];
    [ObservableProperty] private ObservableCollection<RegisteredAudioPreprocessor> _automaticGainControllers = [];
    [ObservableProperty] private ObservableCollection<RegisteredAudioPreprocessor> _echoCancelers = [];
    private bool _disposed;
    private bool _updating;

    public InputSettingsDataViewModel(SettingsService settingsService, AudioService audioService)
    {
        _audioService = audioService;
        _inputSettings = settingsService.InputSettings;
        _settingsService = settingsService;

        _inputSettings.OnUpdated += Update;
        _inputDevice = _inputSettings.InputDevice;
        _inputVolume = _inputSettings.InputVolume;
        _microphoneSensitivity = _inputSettings.MicrophoneSensitivity;
        _denoiser = _inputSettings.Denoiser;
        _automaticGainController = _inputSettings.AutomaticGainController;
        _echoCanceler = _inputSettings.EchoCanceler;
        _pushToTalkEnabled = _inputSettings.PushToTalkEnabled;
        _pushToTalkCue = _inputSettings.PushToTalkCue;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _inputSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public void ReloadDevices()
    {
        InputDevices = [.._audioService.GetInputDevices()];
        Denoisers = [.._audioService.RegisteredAudioPreprocessors.Where(x => x.DenoiserSupported)];
        AutomaticGainControllers = [.._audioService.RegisteredAudioPreprocessors.Where(x => x.GainControllerSupported)];
        EchoCancelers = [.._audioService.RegisteredAudioPreprocessors.Where(x => x.EchoCancelerSupported)];
        
        if (InputDevices.All(outputDevice => outputDevice.Name != InputDevice))
            InputDevice = InputDevices.First(x => x.IsDefault).Name;
        if (Denoisers.FirstOrDefault(x => x.Id == Denoiser) == null)
            Denoiser = Guid.Empty;
        if (AutomaticGainControllers.FirstOrDefault(x => x.Id == AutomaticGainController) == null)
            AutomaticGainController = Guid.Empty;
        if (EchoCancelers.FirstOrDefault(x => x.Id == EchoCanceler) == null)
            EchoCanceler = Guid.Empty;
    }

    partial void OnInputDeviceChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.InputDevice = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnInputVolumeChanging(float value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.InputVolume = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnMicrophoneSensitivityChanging(float value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.MicrophoneSensitivity = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnDenoiserChanging(Guid value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.Denoiser = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnAutomaticGainControllerChanging(Guid value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.AutomaticGainController = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnEchoCancelerChanging(Guid value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.EchoCanceler = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnPushToTalkEnabledChanging(bool value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.PushToTalkEnabled = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }
    
    partial void OnPushToTalkCueChanging(bool value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.PushToTalkCue = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(InputSettings inputSettings)
    {
        if (_updating) return;
        _updating = true;

        InputDevice = inputSettings.InputDevice;
        InputVolume = inputSettings.InputVolume;
        MicrophoneSensitivity = inputSettings.MicrophoneSensitivity;
        Denoiser = inputSettings.Denoiser;
        AutomaticGainController = inputSettings.AutomaticGainController;
        EchoCanceler = inputSettings.EchoCanceler;
        PushToTalkEnabled = inputSettings.PushToTalkEnabled;
        PushToTalkCue = inputSettings.PushToTalkCue;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(InputSettingsDataViewModel).ToString());
    }
}