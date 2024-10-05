using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private IAudioDevices _audioDevices;

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

        public SettingsViewModel(SettingsService settings, ThemesService themes, IAudioDevices audioDevices, IAudioRecorder recorder, IAudioPlayer player)
        {
            _settingsService = settings;
            _themesService = themes;
            _recorder = recorder;
            _player = player;

            _recorder.BufferMilliseconds = 20;

            _audioDevices = audioDevices;
            _themes = new ObservableCollection<string>(themes.ThemeNames);
            _inputDevices = new ObservableCollection<string>(audioDevices.GetWaveInDevices());
            _outputDevices = new ObservableCollection<string>(audioDevices.GetWaveOutDevices());

            _audioSettings = settings.Get<AudioSettings>(Plugin.PluginId);
            _themeSettings = settings.Get<ThemeSettings>(Plugin.PluginId);
            _serversSettings = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);

            //Settings Validation.
            if (!_inputDevices.Contains(_audioSettings.InputDevice))
            {
                _audioSettings.InputDevice = audioDevices.DefaultWaveInDevice();
                _ = _settingsService.SaveAsync();
            }

            if (!_outputDevices.Contains(_audioSettings.OutputDevice))
            {
                _audioSettings.OutputDevice = audioDevices.DefaultWaveOutDevice();
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
                _player.Init(_signal);
                _player.Play();
            }
        }

        [RelayCommand]
        public void TestRecorder()
        {
            if (_recorder.IsRecording)
            {
                _recorder.StopRecording();
                IsRecording = false;
                MicrophoneValue = 0;
            }
            else
            {
                IsRecording = true;
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

        private void UpdateRecorder(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioSettings.InputDevice))
            {
                _recorder.SetDevice(AudioSettings.InputDevice);
                if (_recorder.IsRecording)
                {
                    TestRecorder(); //Stop Recorder.
                }
            }
        }

        private void UpdatePlayer(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioSettings.OutputDevice))
            {
                _player.SetDevice(AudioSettings.OutputDevice);
                if (_player.PlaybackState == PlaybackState.Playing)
                {
                    TestPlayer(); //Stop player.
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

        public override void OnAppearing(object? sender)
        {
            base.OnAppearing(sender);
            InputDevices = new ObservableCollection<string>(_audioDevices.GetWaveInDevices());
            OutputDevices = new ObservableCollection<string>(_audioDevices.GetWaveOutDevices());

            if (!InputDevices.Contains(AudioSettings.InputDevice))
                AudioSettings.InputDevice = _audioDevices.DefaultWaveInDevice();
            if (!InputDevices.Contains(AudioSettings.OutputDevice))
                AudioSettings.InputDevice = _audioDevices.DefaultWaveOutDevice();

            _recorder.DataAvailable += RecordingData;
            ThemeSettings.PropertyChanged += UpdateTheme;
            ThemeSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += UpdateRecorder;
            AudioSettings.PropertyChanged += UpdatePlayer;
            ServersSettings.PropertyChanged += SaveSettings;
            NotificationSettings.PropertyChanged += SaveSettings;
        }

        public override void OnDisappearing(object? sender)
        {
            base.OnDisappearing(sender);
            _recorder.DataAvailable -= RecordingData;
            ThemeSettings.PropertyChanged -= UpdateTheme;
            ThemeSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= UpdateRecorder;
            AudioSettings.PropertyChanged -= UpdatePlayer;
            ServersSettings.PropertyChanged -= SaveSettings;
            NotificationSettings.PropertyChanged -= SaveSettings;

            if (_recorder.IsRecording)
                TestRecorder();

            if (_player.PlaybackState == PlaybackState.Playing)
                TestPlayer();
        }
    }
}