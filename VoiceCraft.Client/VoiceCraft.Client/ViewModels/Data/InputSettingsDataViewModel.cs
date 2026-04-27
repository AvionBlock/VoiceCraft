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

    [ObservableProperty] public partial string InputDevice { get; set; }
    [ObservableProperty] public partial float InputVolume { get; set; }
    [ObservableProperty] public partial float MicrophoneSensitivity { get; set; }
    [ObservableProperty] public partial Guid Denoiser { get; set; }
    [ObservableProperty] public partial Guid AutomaticGainController { get; set; }
    [ObservableProperty] public partial Guid EchoCanceler { get; set; }
    [ObservableProperty] public partial bool HardwarePreprocessorsEnabled { get; set; }
    [ObservableProperty] public partial bool PushToTalkEnabled { get; set; }
    [ObservableProperty] public partial bool PushToTalkCue { get; set; }

    //Lists
    [ObservableProperty] public partial ObservableCollection<AudioDeviceInfo> InputDevices { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<RegisteredAudioPreprocessor> Denoisers { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<RegisteredAudioPreprocessor> AutomaticGainControllers { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<RegisteredAudioPreprocessor> EchoCancelers { get; set; } = [];

    private bool _disposed;
    private bool _updating;

    public InputSettingsDataViewModel(SettingsService settingsService, AudioService audioService)
    {
        _audioService = audioService;
        _inputSettings = settingsService.InputSettings;
        _settingsService = settingsService;

        _inputSettings.OnUpdated += Update;
        InputDevice = _inputSettings.InputDevice;
        InputVolume = _inputSettings.InputVolume;
        MicrophoneSensitivity = _inputSettings.MicrophoneSensitivity;
        Denoiser = _inputSettings.Denoiser;
        AutomaticGainController = _inputSettings.AutomaticGainController;
        EchoCanceler = _inputSettings.EchoCanceler;
        HardwarePreprocessorsEnabled = _inputSettings.HardwarePreprocessorsEnabled;
        PushToTalkEnabled = _inputSettings.PushToTalkEnabled;
        PushToTalkCue = _inputSettings.PushToTalkCue;
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

    partial void OnHardwarePreprocessorsEnabledChanging(bool value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _inputSettings.HardwarePreprocessorsEnabled = value;
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
        HardwarePreprocessorsEnabled = inputSettings.HardwarePreprocessorsEnabled;
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