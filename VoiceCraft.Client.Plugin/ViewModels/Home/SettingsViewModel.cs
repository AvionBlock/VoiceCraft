using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
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
    public partial class SettingsViewModel : ViewModelBase
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
        private IAudioRecorder _recorder;
        private IAudioPlayer _player;

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

        public SettingsViewModel(SettingsService settings, ThemesService themes, IAudioRecorder recorder, IAudioPlayer player)
        {
            _settingsService = settings;
            _themesService = themes;
            _recorder = recorder;
            _player = player;

            _recorder.BufferMilliseconds = 20;

            _themes = new ObservableCollection<string>(themes.ThemeNames);
            _inputDevices = new ObservableCollection<string>(recorder.GetDevices());
            _outputDevices = new ObservableCollection<string>(player.GetDevices());

            _audioSettings = settings.Get<AudioSettings>(Plugin.PluginId);
            _themeSettings = settings.Get<ThemeSettings>(Plugin.PluginId);
            _serversSettings = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);

            //Settings Validation.
            if (!_inputDevices.Contains(_audioSettings.InputDevice))
            {
                _audioSettings.InputDevice = recorder.GetDefaultDevice();
                _ = _settingsService.SaveAsync();
            }

            if (!_outputDevices.Contains(_audioSettings.OutputDevice))
            {
                _audioSettings.OutputDevice = player.GetDefaultDevice();
                _ = _settingsService.SaveAsync();
            }
        }

        [RelayCommand]
        public void TestPlayer()
        {
            if (_player.PlaybackState == PlaybackState.Playing)
            {
                IsPlaying = false;
                _player.Stop();
            }
            else
            {
                IsPlaying = true;
                _player.SetDevice(AudioSettings.OutputDevice);
                _player.Init(_signal);
                _player.Play();
            }
        }

        [RelayCommand]
        public async Task TestRecorder()
        {
            if (_recorder.IsRecording)
            {
                _recorder.StopRecording();
                IsRecording = false;
                MicrophoneValue = 0;
            }
            else
            {
                if (await CheckAndRequestPermission<Permissions.Microphone>() != PermissionStatus.Granted)
                    return;

                IsRecording = true;
                _recorder.SetDevice(AudioSettings.InputDevice);
                _recorder.StartRecording();
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

        private void StopAudio(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioSettings.OutputDevice))
            {
                if (_player.PlaybackState == PlaybackState.Playing)
                {
                    TestPlayer(); //Stop player.
                }
            }
            else if (e.PropertyName == nameof(AudioSettings.InputDevice))
            {
                if (_recorder.IsRecording)
                {
                    _ = TestRecorder();
                }
            }

        }

        private void RecordingData(object? sender, WaveInEventArgs e)
        {
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
            InputDevices = new ObservableCollection<string>(_recorder.GetDevices());
            OutputDevices = new ObservableCollection<string>(_player.GetDevices());

            if (!InputDevices.Contains(AudioSettings.InputDevice))
                AudioSettings.InputDevice = _recorder.GetDefaultDevice();
            if (!OutputDevices.Contains(AudioSettings.OutputDevice))
                AudioSettings.OutputDevice = _player.GetDefaultDevice();

            _recorder.DataAvailable += RecordingData;
            _recorder.RecordingStopped += RecordingStopped;
            ThemeSettings.PropertyChanged += UpdateTheme;
            ThemeSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += StopAudio;
            ServersSettings.PropertyChanged += SaveSettings;
            NotificationSettings.PropertyChanged += SaveSettings;
        }

        public override void OnDisappearing(object? sender)
        {
            base.OnDisappearing(sender);
            _recorder.DataAvailable -= RecordingData;
            _recorder.RecordingStopped -= RecordingStopped;
            ThemeSettings.PropertyChanged -= UpdateTheme;
            ThemeSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= StopAudio;
            ServersSettings.PropertyChanged -= SaveSettings;
            NotificationSettings.PropertyChanged -= SaveSettings;

            if (_recorder.IsRecording)
                _ = TestRecorder(); //Shutup

            if (_player.PlaybackState == PlaybackState.Playing)
                TestPlayer();
        }

        public async Task<PermissionStatus> CheckAndRequestPermission<TPermission>(string? rationalDescription = null) where TPermission : Permissions.BasePermission, new()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<TPermission>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<TPermission>() && !string.IsNullOrWhiteSpace(rationalDescription))
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<TPermission>();

            return status;
        }
    }
}