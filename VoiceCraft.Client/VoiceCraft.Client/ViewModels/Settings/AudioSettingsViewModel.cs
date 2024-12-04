using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class AudioSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly AudioSettings _audioSettings;

        [ObservableProperty] private string _inputDevice;
        [ObservableProperty] private string _outputDevice;
        [ObservableProperty] private Guid _preprocessor;
        [ObservableProperty] private Guid _echoCanceler;
        [ObservableProperty] private float _microphoneSensitivity;
        [ObservableProperty] private bool _aec;
        [ObservableProperty] private bool _agc;
        [ObservableProperty] private bool _denoiser;
        [ObservableProperty] private bool _vad;

        public AudioSettingsViewModel(AudioSettings audioSettings)
        {
            _audioSettings = audioSettings;
            _audioSettings.OnUpdated += Update;
            _inputDevice = _audioSettings.InputDevice;
            _outputDevice = _audioSettings.OutputDevice;
            _preprocessor = _audioSettings.Preprocessor;
            _echoCanceler = _audioSettings.EchoCanceler;
            _microphoneSensitivity = _audioSettings.MicrophoneSensitivity;
            _aec = _audioSettings.AEC;
            _agc = _audioSettings.AGC;
            _denoiser = _audioSettings.Denoiser;
            _vad = _audioSettings.VAD;
        }

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            
            _audioSettings.InputDevice = InputDevice;
            _audioSettings.OutputDevice = OutputDevice;
            _audioSettings.Preprocessor = Preprocessor;
            _audioSettings.EchoCanceler = EchoCanceler;
            _audioSettings.MicrophoneSensitivity = MicrophoneSensitivity;
            _audioSettings.AEC = Aec;
            _audioSettings.AGC = Agc;
            _audioSettings.Denoiser = Denoiser;
            _audioSettings.VAD = Vad;
            
            base.OnPropertyChanging(e);
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
            Aec = audioSettings.AEC;
            Agc = audioSettings.AGC;
            Denoiser = audioSettings.Denoiser;
            Vad = audioSettings.VAD;
            
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