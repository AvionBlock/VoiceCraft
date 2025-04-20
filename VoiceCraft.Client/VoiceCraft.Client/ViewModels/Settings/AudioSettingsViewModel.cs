using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class AudioSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly AudioSettings _audioSettings;
        private readonly SettingsService _settingsService;
        private readonly AudioService _audioService;

        [ObservableProperty] private ObservableCollection<string> _inputDevices = [];
        [ObservableProperty] private ObservableCollection<string> _outputDevices = [];
        [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = [];
        [ObservableProperty] private ObservableCollection<RegisteredAutomaticGainController> _automaticGainControllers = [];
        [ObservableProperty] private ObservableCollection<RegisteredDenoiser> _denoisers = [];

        [ObservableProperty] private string _inputDevice;
        [ObservableProperty] private string _outputDevice;
        [ObservableProperty] private Guid _denoiser;
        [ObservableProperty] private Guid _echoCanceler;
        [ObservableProperty] private Guid _automaticGainController;
        [ObservableProperty] private float _microphoneSensitivity;

        public AudioSettingsViewModel(SettingsService settingsService, AudioService audioService)
        {
            _audioSettings = settingsService.AudioSettings;
            _settingsService = settingsService;
            _audioService = audioService;
            
            _audioSettings.OnUpdated += Update;
            _inputDevice = _audioSettings.InputDevice;
            _outputDevice = _audioSettings.OutputDevice;
            _denoiser = _audioSettings.Denoiser;
            _automaticGainController = _audioSettings.AutomaticGainController;
            _echoCanceler = _audioSettings.EchoCanceler;
            _microphoneSensitivity = _audioSettings.MicrophoneSensitivity;
            
            ReloadAvailableDevices();
        }

        public void ReloadAvailableDevices()
        {
            InputDevices = ["Default", .._audioService.GetInputDevices()];
            OutputDevices = ["Default", .._audioService.GetOutputDevices()];
            Denoisers = new ObservableCollection<RegisteredDenoiser>(_audioService.RegisteredDenoisers);
            AutomaticGainControllers = new ObservableCollection<RegisteredAutomaticGainController>(_audioService.RegisteredAutomaticGainControllers);
            EchoCancelers = new ObservableCollection<RegisteredEchoCanceler>(_audioService.RegisteredEchoCancelers);
            
            if(!InputDevices.Contains(InputDevice))
                InputDevice = "Default";
            if(!OutputDevices.Contains(OutputDevice))
                OutputDevice = "Default";
            if(Denoisers.FirstOrDefault(x => x.Id == Denoiser) == null)
                Denoiser = Guid.Empty;
            if(AutomaticGainControllers.FirstOrDefault(x => x.Id == AutomaticGainController) == null)
                AutomaticGainController = Guid.Empty;
            if(EchoCancelers.FirstOrDefault(x => x.Id == EchoCanceler) == null)
                EchoCanceler = Guid.Empty;
        }

        partial void OnInputDeviceChanging(string value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.InputDevice = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnOutputDeviceChanging(string value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.OutputDevice = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnDenoiserChanging(Guid value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Denoiser = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnAutomaticGainControllerChanging(Guid value)
        {
            ThrowIfDisposed();
            
            if(_updating) return;
            _updating = true;
            _audioSettings.AutomaticGainController = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }
        
        partial void OnEchoCancelerChanging(Guid value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.EchoCanceler = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnMicrophoneSensitivityChanging(float value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.MicrophoneSensitivity = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(AudioSettings audioSettings)
        {
            if (_updating) return;
            _updating = true;
            
            InputDevice = audioSettings.InputDevice;
            OutputDevice = audioSettings.OutputDevice;
            Denoiser = audioSettings.Denoiser;
            AutomaticGainController = audioSettings.AutomaticGainController;
            EchoCanceler = audioSettings.EchoCanceler;
            MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
            
            _updating = false;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(AudioSettingsViewModel).ToString());
        }
        
        public void Dispose()
        {
            if(_disposed) return;
            _audioSettings.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}