using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    [ObservableProperty] private float _microphoneSensitivity;
    [ObservableProperty] private Guid _denoiser;
    [ObservableProperty] private Guid _automaticGainController;
    [ObservableProperty] private Guid _echoCanceler;

    //Lists
    [ObservableProperty] private ObservableCollection<string> _inputDevices = [];
    [ObservableProperty] private ObservableCollection<RegisteredDenoiser> _denoisers = [];
    [ObservableProperty] private ObservableCollection<RegisteredAutomaticGainController> _automaticGainControllers = [];
    [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = [];
    private bool _disposed;
    private bool _updating;

    public InputSettingsDataViewModel(SettingsService settingsService, AudioService audioService)
    {
        _inputSettings = settingsService.InputSettings;
        _audioService = audioService;
        _settingsService = settingsService;

        _inputSettings.OnUpdated += Update;
        _inputDevice = _inputSettings.InputDevice;
        _microphoneSensitivity = _inputSettings.MicrophoneSensitivity;
        _denoiser = _inputSettings.Denoiser;
        _automaticGainController = _inputSettings.AutomaticGainController;
        _echoCanceler = _inputSettings.EchoCanceler;

        _ = ReloadAvailableDevices();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _inputSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async Task ReloadAvailableDevices()
    {
        InputDevices = ["Default", ..await _audioService.GetInputDevicesAsync()];
        Denoisers = new ObservableCollection<RegisteredDenoiser>(_audioService.RegisteredDenoisers);
        AutomaticGainControllers =
            new ObservableCollection<RegisteredAutomaticGainController>(
                _audioService.RegisteredAutomaticGainControllers);
        EchoCancelers = new ObservableCollection<RegisteredEchoCanceler>(_audioService.RegisteredEchoCancelers);

        if (!InputDevices.Contains(InputDevice))
            InputDevice = "Default";
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

    private void Update(InputSettings inputSettings)
    {
        if (_updating) return;
        _updating = true;

        InputDevice = inputSettings.InputDevice;
        MicrophoneSensitivity = inputSettings.MicrophoneSensitivity;
        Denoiser = inputSettings.Denoiser;
        AutomaticGainController = inputSettings.AutomaticGainController;
        EchoCanceler = inputSettings.EchoCanceler;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(InputSettingsDataViewModel).ToString());
    }
}