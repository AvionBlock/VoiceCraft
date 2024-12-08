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
        [ObservableProperty] private ObservableCollection<RegisteredPreprocessor> _preprocessors = [];
        [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = [];

        [ObservableProperty] private string _inputDevice;
        [ObservableProperty] private string _outputDevice;
        [ObservableProperty] private Guid _preprocessor;
        [ObservableProperty] private Guid _echoCanceler;
        [ObservableProperty] private float _microphoneSensitivity;
        [ObservableProperty] private bool _aec;
        [ObservableProperty] private bool _agc;
        [ObservableProperty] private bool _denoiser;
        [ObservableProperty] private bool _vad;

        public AudioSettingsViewModel(AudioSettings audioSettings, SettingsService settingsService, AudioService audioService)
        {
            _audioSettings = audioSettings;
            _settingsService = settingsService;
            _audioService = audioService;
            
            _audioSettings.OnUpdated += Update;
            _inputDevice = _audioSettings.InputDevice;
            _outputDevice = _audioSettings.OutputDevice;
            _preprocessor = _audioSettings.Preprocessor;
            _echoCanceler = _audioSettings.EchoCanceler;
            _microphoneSensitivity = _audioSettings.MicrophoneSensitivity;
            _aec = _audioSettings.Aec;
            _agc = _audioSettings.Agc;
            _denoiser = _audioSettings.Denoiser;
            _vad = _audioSettings.Vad;
            
            ReloadAvailableDevices();
        }

        public void ReloadAvailableDevices()
        {
            InputDevices = ["Default", .._audioService.GetInputDevices()];
            OutputDevices = ["Default", .._audioService.GetOutputDevices()];
            Preprocessors = new ObservableCollection<RegisteredPreprocessor>(_audioService.RegisteredPreprocessors);
            EchoCancelers = new ObservableCollection<RegisteredEchoCanceler>(_audioService.RegisteredEchoCancelers);
            
            if(!InputDevices.Contains(InputDevice))
                InputDevice = "Default";
            if(!OutputDevices.Contains(OutputDevice))
                OutputDevice = "Default";
            if(Preprocessors.FirstOrDefault(x => x.Id == Preprocessor) == null)
                Preprocessor = Guid.Empty;
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

        partial void OnPreprocessorChanging(Guid value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Preprocessor = value;
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

        partial void OnAecChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Aec = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnAgcChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Agc = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnDenoiserChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Denoiser = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnVadChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _audioSettings.Vad = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(AudioSettings audioSettings)
        {
            if (_updating) return;
            _updating = true;
            
            InputDevice = audioSettings.InputDevice;
            OutputDevice = audioSettings.OutputDevice;
            Preprocessor = audioSettings.Preprocessor;
            EchoCanceler = audioSettings.EchoCanceler;
            MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
            Aec = audioSettings.Aec;
            Agc = audioSettings.Agc;
            Denoiser = audioSettings.Denoiser;
            Vad = audioSettings.Vad;
            
            _updating = false;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(ServerViewModel));
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