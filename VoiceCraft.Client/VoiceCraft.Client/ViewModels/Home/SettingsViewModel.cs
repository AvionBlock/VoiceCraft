using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel : ViewModelBase, IDisposable
    {
        private readonly AudioService _audioService;
        private readonly NotificationService _notificationService;
        private readonly PermissionsService _permissionsService;
        private readonly SineWaveGenerator _sineWaveGenerator;
        private IAudioRecorder? _recorder;
        private IAudioPlayer? _player;
        private IDenoiser? _denoiser;
        private IEchoCanceler? _echoCanceler;
        private IAutomaticGainController? _gainController;
        
        //General Settings
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
        
        //Advanced
        [ObservableProperty] private bool _advancedSettingsExpanded;

        public SettingsViewModel(
            ThemesService themesService,
            SettingsService settingsService,
            AudioService audioService,
            NotificationService notificationService,
            PermissionsService permissionsService)
        {
            _audioService = audioService;
            _notificationService = notificationService;
            _permissionsService = permissionsService;
            _sineWaveGenerator = new SineWaveGenerator(Constants.SampleRate);

            _locales = new ObservableCollection<string>(Localizer.Languages);
            _themes = new ObservableCollection<RegisteredTheme>(themesService.RegisteredThemes);
            _backgroundImages = new ObservableCollection<RegisteredBackgroundImage>(themesService.RegisteredBackgroundImages);
            _localeSettings = new LocaleSettingsViewModel(settingsService);
            _themeSettings = new ThemeSettingsViewModel(settingsService, themesService);
            _notificationSettings = new NotificationSettingsViewModel(settingsService);
            _serversSettings = new ServersSettingsViewModel(settingsService);
            _audioSettings = new AudioSettingsViewModel(settingsService, _audioService);
        }

        [RelayCommand]
        private async Task TestRecorder()
        {
            try
            {
                if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                        "VoiceCraft requires the microphone permission to be granted in order to test recording!") !=
                    PermissionStatus.Granted)
                {
                    throw new InvalidOperationException("Could not create recorder, Microphone permission not granted.");
                }

                if (CleanupRecorder()) return;
                
                _recorder = _audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
                _recorder.BufferMilliseconds = Constants.FrameSizeMs;
                _recorder.SelectedDevice = AudioSettings.InputDevice == "Default" ? null : AudioSettings.InputDevice;
                _recorder.OnDataAvailable += OnDataAvailable;
                _recorder.OnRecordingStopped += OnRecordingStopped;
                _recorder.Initialize();

                _gainController = _audioService.GetAutomaticGainController(AudioSettings.AutomaticGainController)?.Instantiate();
                _gainController?.Initialize(_recorder);
                
                _denoiser = _audioService.GetDenoiser(AudioSettings.Denoiser)?.Instantiate();
                _denoiser?.Initialize(_recorder);

                if (_player != null)
                {
                    _echoCanceler = _audioService.GetEchoCanceler(AudioSettings.EchoCanceler)?.Instantiate();
                    _echoCanceler?.Initialize(_recorder, _player);
                }

                _recorder.Start();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                CleanupRecorder();
                _notificationService.SendErrorNotification(ex.Message);
            }
        }

        [RelayCommand]
        private void TestPlayer()
        {
            try
            {
                if (CleanupPlayer()) return;
                
                _player = _audioService.CreateAudioPlayer(Constants.SampleRate, Constants.Channels, Constants.Format);
                _player.SelectedDevice = AudioSettings.OutputDevice == "Default" ? null : AudioSettings.OutputDevice;
                _player.BufferMilliseconds = 100;
                _player.OnPlaybackStopped += OnPlaybackStopped;
                _player.Initialize(Read);
                _player.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                CleanupPlayer();
                IsPlaying = false;
                _notificationService.SendErrorNotification(ex.Message);
            }
        }

        private int Read(byte[] buffer, int offset, int count)
        {
            var read = _sineWaveGenerator.Read(buffer, offset, count);
            _echoCanceler?.EchoPlayback(buffer, count);
            return read;
        }

        [RelayCommand]
        private void TriggerGc()
        {
            try
            {
                var previousSnapshot = GC.GetTotalMemory(false);
                GC.Collect();
                _notificationService.SendNotification(
                    $"Garbage Collection Triggered. Memory Cleared: {Math.Max(previousSnapshot - GC.GetTotalMemory(false), 0) / 1000000}mb");
            }
            catch (Exception ex)
            {
                _notificationService.SendErrorNotification(ex.Message);
            }
        }

        private void OnDataAvailable(byte[] data, int count)
        {
            _echoCanceler?.EchoCancel(data, count);
            _gainController?.Process(data);
            _denoiser?.Denoise(data);
            
            float max = 0;
            // interpret as 16-bit audio
            for (var index = 0; index < data.Length; index += 2)
            {
                var sample = (short)((data[index + 1] << 8) |
                                     data[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            
            MicrophoneValue = max;
            DetectingVoiceActivity = max >= AudioSettings.MicrophoneSensitivity;
        }
        
        private void OnRecordingStopped(Exception? ex)
        {
            CleanupRecorder();
            
            if(ex != null)
                _notificationService.SendErrorNotification(ex.Message);
        }
        
        private void OnPlaybackStopped(Exception? ex)
        {
            CleanupPlayer();
            
            if(ex != null)
                _notificationService.SendErrorNotification(ex.Message);
        }
        
        private bool CleanupRecorder()
        {
            if (_recorder == null) return false;
            var recorder = _recorder;
            _recorder = null;
            
            recorder.OnRecordingStopped -= OnRecordingStopped;
            recorder.OnDataAvailable -= OnDataAvailable;
            recorder.Dispose();
            _gainController?.Dispose();
            _denoiser?.Dispose();
            _recorder = null;
            _gainController = null;
            _denoiser = null;
            IsRecording = false;
            MicrophoneValue = 0;
            DetectingVoiceActivity = false;
            return true;
        }

        private bool CleanupPlayer()
        {
            if (_player == null) return false;
            var player = _player;
            _player = null;
            
            player.OnPlaybackStopped -= OnPlaybackStopped;
            player.Dispose();
            IsPlaying = false;
            return true;
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            AudioSettings.ReloadAvailableDevices();
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();
            CleanupRecorder();
            CleanupPlayer();
        }

        public void Dispose()
        {
            ThemeSettings.Dispose();
            NotificationSettings.Dispose();
            ServersSettings.Dispose();
            AudioSettings.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}