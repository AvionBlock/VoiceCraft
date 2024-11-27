using Avalonia.Controls;
using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VoiceCraft.Client.PDK.Audio;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class SettingsViewModel : ViewModelBase, IDisposable
    {
        public override string Title => "Settings";

        private SignalGenerator _signal = new SignalGenerator(48000, 2)
        {
            Gain = 0.2,
            Frequency = 500,
            Type = SignalGeneratorType.Sin
        };
        private ThemesService _themesService;
        private SettingsService _settingsService;
        private AudioService _audioService;
        private PermissionsService _permissionsService;
        private INotificationMessageManager _manager;
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;
        private IPreprocessor? _preprocessor;

        [ObservableProperty]
        private bool _audioSettingsExpanded = false;

        [ObservableProperty]
        private bool _generalSettingsExpanded = false;

        [ObservableProperty]
        private ObservableCollection<string> _themes;

        [ObservableProperty]
        private ObservableCollection<string> _inputDevices;

        [ObservableProperty]
        private ObservableCollection<string> _outputDevices;

        [ObservableProperty]
        private ObservableCollection<string> _preprocessors;

        [ObservableProperty]
        private ObservableCollection<string> _echoCancellers;

        [ObservableProperty]
        private AudioSettings _audioSettings;

        [ObservableProperty]
        private ThemeSettings _themeSettings;

        [ObservableProperty]
        private ServersSettings _serversSettings;

        [ObservableProperty]
        private NotificationSettings _notificationSettings;

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private bool _isPlaying = false;

        [ObservableProperty]
        private float _microphoneValue;

        public SettingsViewModel(SettingsService settings, ThemesService themes, AudioService audioService, PermissionsService permissions, NotificationMessageManager manager)
        {
            _permissionsService = permissions;
            _settingsService = settings;
            _themesService = themes;
            _audioService = audioService;
            _manager = manager;

            _themes = new ObservableCollection<string>(themes.ThemeNames);
            _inputDevices = new ObservableCollection<string>(_audioService.GetInputDevices());
            _outputDevices = new ObservableCollection<string>(_audioService.GetInputDevices());
            _preprocessors = new ObservableCollection<string>(_audioService.GetPreprocessors());
            _echoCancellers = new ObservableCollection<string>(_audioService.GetEchoCancellers());

            _audioSettings = settings.Get<AudioSettings>(Plugin.PluginId);
            _themeSettings = settings.Get<ThemeSettings>(Plugin.PluginId);
            _serversSettings = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);

            //Settings Validation.
            if (!_inputDevices.Contains(_audioSettings.InputDevice))
            {
                _audioSettings.InputDevice = _audioService.GetDefaultInputDevice();
                _ = _settingsService.SaveAsync();
            }

            if (!_outputDevices.Contains(_audioSettings.OutputDevice))
            {
                _audioSettings.OutputDevice = _audioService.GetDefaultOutputDevice();
                _ = _settingsService.SaveAsync();
            }
        }

        [RelayCommand]
        public void TestPlayer()
        {
            try
            {
                if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
                {
                    IsPlaying = false;
                    _audioPlayer.Stop();
                    _audioPlayer.Dispose();
                    _audioPlayer = null;
                }
                else
                {
                    IsPlaying = true;
                    if (_audioPlayer != null)
                    {
                        _audioPlayer.Dispose();
                        _audioPlayer = null;
                    }

                    _audioPlayer = _audioService.CreateAudioPlayer();
                    _audioPlayer.SetDevice(AudioSettings.OutputDevice);
                    _audioPlayer.Init(_signal);
                    _audioPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(ex.Message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(NotificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }

        [RelayCommand]
        public async Task TestRecorder()
        {
            try
            {
                if (_audioRecorder != null && _audioRecorder.IsRecording)
                {
                    IsRecording = false;
                    _audioRecorder.StopRecording();
                    _audioRecorder.DataAvailable -= RecordingData;
                    _audioRecorder.RecordingStopped -= RecordingStopped;
                    _audioRecorder.Dispose();
                    _preprocessor?.Dispose();
                    _preprocessor = null;
                    _audioRecorder = null;
                    MicrophoneValue = 0;
                }
                else
                {
                    if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>() != PermissionStatus.Granted)
                        return;

                    if (_audioRecorder != null)
                    {
                        _audioRecorder.DataAvailable -= RecordingData;
                        _audioRecorder.RecordingStopped -= RecordingStopped;
                        _audioRecorder.Dispose();
                        _preprocessor?.Dispose();
                        _preprocessor = null;
                        _audioRecorder = null;
                    }

                    _preprocessor = _audioService.CreatePreprocessor(AudioSettings.Preprocessor);
                    if (_preprocessor != null)
                    {
                        _preprocessor.GainControllerEnabled = AudioSettings.Agc;
                        _preprocessor.NoiseSuppressorEnabled = AudioSettings.Denoiser;

                        if (AudioSettings.Agc && !_preprocessor.IsGainControllerAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Automatic Gain Control. No gain adjustment will be applied!");
                        if (AudioSettings.Denoiser && !_preprocessor.IsNoiseSuppressorAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Noise Suppression. No denoising will be applied!");
                        if (AudioSettings.Vad && !_preprocessor.IsVoiceActivityDetectionAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Voice Activity Detection. Audio will always be detected!");
                    }

                    _audioRecorder = _audioService.CreateAudioRecorder();
                    IsRecording = true;
                    _audioRecorder.BufferMilliseconds = 20;
                    _audioRecorder.SetDevice(AudioSettings.InputDevice);
                    _audioRecorder.DataAvailable += RecordingData;
                    _audioRecorder.RecordingStopped += RecordingStopped;
                    _audioRecorder.StartRecording();
                    _preprocessor?.Init(_audioRecorder);
                }
            }
            catch (Exception ex)
            {
                _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(ex.Message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(NotificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }

        private void SaveSettings(object? sender, PropertyChangedEventArgs e)
        {
            _ = _settingsService.SaveAsync();
        }

        private void UpdateTheme(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeSettings.SelectedTheme))
            {
                _themesService.SwitchTheme(ThemeSettings.SelectedTheme);
            }
        }

        private void UpdateAudio(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioSettings.OutputDevice))
            {
                if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
                {
                    TestPlayer(); //Stop player.
                }
            }
            else if (e.PropertyName == nameof(AudioSettings.InputDevice))
            {
                if (_audioRecorder != null && _audioRecorder.IsRecording)
                {
                    _ = TestRecorder();
                }
            }
            else if (e.PropertyName == nameof(AudioSettings.Preprocessor))
            {
                if (_audioRecorder != null && _audioRecorder.IsRecording)
                {
                    _ = TestRecorder();
                }
            }
            else if (e.PropertyName == nameof(AudioSettings.EchoCanceller))
            {
                if (_audioRecorder != null && _audioRecorder.IsRecording)
                {
                    _ = TestRecorder();
                }
            }

            if (_preprocessor != null)
            {
                if (e.PropertyName == nameof(AudioSettings.Agc))
                {
                    if (AudioSettings.Agc && !_preprocessor.IsGainControllerAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Automatic Gain Control. No gain adjustment will be applied!");
                    _preprocessor.GainControllerEnabled = AudioSettings.Agc;
                }
                if (e.PropertyName == nameof(AudioSettings.Denoiser))
                {
                    if (AudioSettings.Denoiser && !_preprocessor.IsNoiseSuppressorAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Noise Suppression. No denoising will be applied!");
                    _preprocessor.NoiseSuppressorEnabled = AudioSettings.Denoiser;
                }
                if (e.PropertyName == nameof(AudioSettings.Vad))
                {
                    if (AudioSettings.Vad && !_preprocessor.IsVoiceActivityDetectionAvailable) SendWarningNotification($"{AudioSettings.Preprocessor} does not support Voice Activity Detection. Audio will always be detected!");
                    _preprocessor.VoiceActivityDetectionEnabled = AudioSettings.Vad;
                }
            }
        }

        private void RecordingData(object? sender, WaveInEventArgs e)
        {
            var vad = true;
            if (_preprocessor?.Initialized ?? false)
                vad = _preprocessor?.Process(e.Buffer) ?? true;

            if (AudioSettings.Vad)
            {
                MicrophoneValue = vad ? 1 : 0;
                return;
            }

            //Can't really test echo canceller.
            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                // is this the max value?
                if (sample32 > max) max = sample32;
            }

            MicrophoneValue = max;
        }

        private void RecordingStopped(object? sender, StoppedEventArgs e)
        {
            IsRecording = false;
        }

        public override void OnAppearing(object? sender)
        {
            base.OnAppearing(sender);
            InputDevices = new ObservableCollection<string>(_audioService.GetInputDevices());
            OutputDevices = new ObservableCollection<string>(_audioService.GetOutputDevices());
            Preprocessors = new ObservableCollection<string>(_audioService.GetPreprocessors());
            EchoCancellers = new ObservableCollection<string>(_audioService.GetEchoCancellers());

            if (!InputDevices.Contains(AudioSettings.InputDevice))
                AudioSettings.InputDevice = _audioService.GetDefaultInputDevice();
            if (!OutputDevices.Contains(AudioSettings.OutputDevice))
                AudioSettings.OutputDevice = _audioService.GetDefaultOutputDevice();


            ThemeSettings.PropertyChanged += UpdateTheme;
            ThemeSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += UpdateAudio;
            ServersSettings.PropertyChanged += SaveSettings;
            NotificationSettings.PropertyChanged += SaveSettings;
        }

        public override void OnDisappearing(object? sender)
        {
            base.OnDisappearing(sender);
            ThemeSettings.PropertyChanged -= UpdateTheme;
            ThemeSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= UpdateAudio;
            ServersSettings.PropertyChanged -= SaveSettings;
            NotificationSettings.PropertyChanged -= SaveSettings;

            if (_audioRecorder != null && _audioRecorder.IsRecording)
                _ = TestRecorder(); //Shutup

            if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
                TestPlayer();
        }

        public void Dispose()
        {
            if (_audioRecorder != null)
            {
                _audioRecorder.DataAvailable -= RecordingData;
                _audioRecorder.RecordingStopped -= RecordingStopped;
                _audioRecorder.Dispose();
                _audioRecorder = null;
            }
            if (_audioPlayer != null)
            {
                _audioPlayer.Dispose();
                _audioPlayer = null;
            }
            if (_preprocessor != null)
            {
                _preprocessor.Dispose();
                _preprocessor = null;
            }
        }

        private void SendWarningNotification(string message)
        {
            _manager.CreateMessage()
                .Accent(ThemesService.GetBrushResource("notificationAccentBrush"))
                .Animates(true)
                .Background(ThemesService.GetBrushResource("notificationBackgroundBrush"))
                .HasBadge("Warning")
                .HasMessage(message)
                .Dismiss().WithDelay(TimeSpan.FromMilliseconds(NotificationSettings.DismissDelayMS))
                .Dismiss().WithButton("Dismiss", (button) => { })
                .Queue();
        }
    }
}