using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using Microsoft.Maui.ApplicationModel;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly AudioService _audioService;
        private readonly NotificationService _notificationService;
        private readonly PermissionsService _permissionsService;
        private readonly BackgroundService _backgroundService;

        private readonly SignalGenerator _signal = new(48000, 2)
        {
            Gain = 0.2,
            Frequency = 500,
            Type = SignalGeneratorType.Sin
        };

        private IAudioRecorder? _recorder;
        private IAudioPlayer? _player;
        private IDenoiser? _denoiser;
        private IAutomaticGainController? _automaticGainController;

        [ObservableProperty] private bool _generalSettingsExpanded;
        
        //Language Settings
        [ObservableProperty] private LocaleSettingsViewModel _localeSettings;

        //Theme Settings
        [ObservableProperty] private ObservableCollection<string> _locales;
        [ObservableProperty] private ObservableCollection<RegisteredTheme> _themes;
        [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages;
        [ObservableProperty] private ThemeSettingsViewModel _themeSettings;

        //Notification Settings
        [ObservableProperty] private NotificationSettingsViewModel _notificationSettings;

        //Server Settings
        [ObservableProperty] private ServersSettingsViewModel _serversSettings;

        //Audio Settings
        [ObservableProperty] private bool _audioSettingsExpanded;
        [ObservableProperty] private AudioSettingsViewModel _audioSettings;

        //Testers
        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private float _microphoneValue;
        [ObservableProperty] private bool _detectingVoiceActivity;

        public SettingsViewModel(
            ThemesService themesService,
            SettingsService settingsService,
            AudioService audioService,
            NotificationService notificationService,
            PermissionsService permissionsService,
            BackgroundService backgroundService)
        {
            _audioService = audioService;
            _notificationService = notificationService;
            _permissionsService = permissionsService;
            _backgroundService = backgroundService;

            _locales = new ObservableCollection<string>(Localizer.Languages);
            _themes = new ObservableCollection<RegisteredTheme>(themesService.RegisteredThemes);
            _backgroundImages = new ObservableCollection<RegisteredBackgroundImage>(themesService.RegisteredBackgroundImages);
            _localeSettings = new LocaleSettingsViewModel(settingsService.Get<LocaleSettings>(), settingsService);
            _themeSettings = new ThemeSettingsViewModel(settingsService.Get<ThemeSettings>(), settingsService, themesService);
            _notificationSettings = new NotificationSettingsViewModel(settingsService.Get<NotificationSettings>(), settingsService);
            _serversSettings = new ServersSettingsViewModel(settingsService.Get<ServersSettings>(), settingsService);
            _audioSettings = new AudioSettingsViewModel(settingsService.Get<AudioSettings>(), settingsService, _audioService);
        }

        [RelayCommand]
        private async Task TestRecorder()
        {
            try
            {
                if (_recorder != null)
                {
                    _recorder.Dispose();
                }
                else
                {
                    if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                            "VoiceCraft requires the microphone permission to be granted in order to test recording!") !=
                        PermissionStatus.Granted)
                    {
                        IsRecording = false;
                        return;
                    }

                    _recorder = _audioService.CreateAudioRecorder();
                    _recorder.BufferMilliseconds = 20;
                    _recorder.WaveFormat = new WaveFormat(48000, 1);
                    _recorder.SelectedDevice =
                        AudioSettings.InputDevice == "Default" ? null : AudioSettings.InputDevice;

                    var denoiser = _audioService.GetDenoiser(AudioSettings.Denoiser);
                    _denoiser = denoiser?.Type == null? null : denoiser.Instantiate();
                    var automaticGainController = _audioService.GetAutomaticGainController(AudioSettings.AutomaticGainController);
                    _automaticGainController = automaticGainController?.Type == null? null : automaticGainController.Instantiate();
                    
                    //Can't really test echo cancellation.
                    _recorder.DataAvailable += OnDataAvailable;
                    _recorder.RecordingStopped += OnRecordingStopped;
                    _recorder.StartRecording();
                    _denoiser?.Init(_recorder);
                    _automaticGainController?.Init(_recorder);
                    IsRecording = true;
                }
            }
            catch (Exception ex)
            {
                _notificationService.SendErrorNotification(ex.Message);
                _recorder?.Dispose();
                _denoiser?.Dispose();
                _automaticGainController?.Dispose();
                _recorder = null;
                _denoiser = null;
                _automaticGainController = null;
                MicrophoneValue = 0;
                DetectingVoiceActivity = false;
                IsRecording = false;
            }
        }

        [RelayCommand]
        private void TestPlayer()
        {
            try
            {
                if (_player != null)
                {
                    _player.Dispose();
                }
                else
                {
                    _player = _audioService.CreateAudioPlayer();
                    _player.SelectedDevice =
                        AudioSettings.OutputDevice == "Default" ? null : AudioSettings.OutputDevice;

                    _player.PlaybackStopped += OnPlaybackStopped;
                    _player.Init(_signal);
                    _player.Play();
                    IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                _notificationService.SendErrorNotification(ex.Message);
                _player?.Dispose();
                _player = null;
                IsPlaying = false;
            }
        }

        [RelayCommand]
        private void Test()
        {
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                _notificationService.SendErrorNotification(e.Exception.Message);

            _recorder?.Dispose();
            _denoiser?.Dispose();
            _automaticGainController?.Dispose();
            _recorder = null;
            _denoiser = null;
            _automaticGainController = null;
            MicrophoneValue = 0;
            DetectingVoiceActivity = false;
            IsRecording = false;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                _notificationService.SendErrorNotification(e.Exception.Message);

            _player?.Dispose();
            _player = null;
            IsPlaying = false;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            _denoiser?.Denoise(e.Buffer);
            _automaticGainController?.Process(e.Buffer);
            float max = 0;
            // interpret as 16-bit audio
            for (var index = 0; index < e.BytesRecorded; index += 2)
            {
                var sample = (short)((e.Buffer[index + 1] << 8) |
                                     e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value
                if (sample32 < 0) sample32 = -sample32;
                // is this the max value?
                if (sample32 > max) max = sample32;
            }

            MicrophoneValue = max;
            DetectingVoiceActivity = MicrophoneValue > AudioSettings.MicrophoneSensitivity;
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            AudioSettings.ReloadAvailableDevices();
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();
            _recorder?.Dispose();
            _recorder = null;
            _player?.Dispose();
            _player = null;
        }

        public void Dispose()
        {
            ThemeSettings.Dispose();
            NotificationSettings.Dispose();
            ServersSettings.Dispose();
            AudioSettings.Dispose();
            _recorder?.Dispose();
            _player?.Dispose();
            _denoiser?.Dispose();
            _automaticGainController?.Dispose();
            _recorder = null;
            _player = null;
            _denoiser = null;
            _automaticGainController = null;
            GC.SuppressFinalize(this);
        }
    }
}