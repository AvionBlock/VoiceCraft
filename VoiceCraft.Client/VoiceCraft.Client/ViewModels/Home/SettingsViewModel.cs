using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel(
        ThemesService themesService,
        SettingsService settingsService,
        AudioService audioService,
        NotificationService notificationService,
        PermissionsService permissionsService) : ViewModelBase, IDisposable
    {
        private readonly SignalGenerator _signal = new(48000, 2)
        {
            Gain = 0.2,
            Frequency = 500,
            Type = SignalGeneratorType.Sin
        };

        private IAudioRecorder? _recorder;
        private IAudioPlayer? _player;

        [ObservableProperty] private bool _generalSettingsExpanded;

        //Theme Settings
        [ObservableProperty]
        private ObservableCollection<RegisteredTheme> _themes = new(themesService.RegisteredThemes);

        [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages =
            new(themesService.RegisteredBackgroundImages);

        [ObservableProperty] private ThemeSettingsViewModel _themeSettings =
            new(settingsService.Get<ThemeSettings>(), settingsService, themesService);

        //Notification Settings
        [ObservableProperty] private NotificationSettingsViewModel _notificationSettings =
            new(settingsService.Get<NotificationSettings>(), settingsService);

        //Server Settings
        [ObservableProperty] private ServersSettingsViewModel
            _serversSettings = new(settingsService.Get<ServersSettings>(), settingsService);

        //Audio Settings
        [ObservableProperty] private bool _audioSettingsExpanded;

        [ObservableProperty] private AudioSettingsViewModel _audioSettings =
            new(settingsService.Get<AudioSettings>(), settingsService, audioService);

        //Testers
        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private float _microphoneValue;

        [RelayCommand]
        private async Task TestRecorder()
        {
            try
            {
                if (_recorder != null)
                {
                    _recorder.StopRecording();
                    _recorder.Dispose();
                    _recorder = null;
                    MicrophoneValue = 0;
                    IsRecording = false;
                }
                else
                {
                    if (await permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                            "VoiceCraft requires the microphone permission to be granted in order to test recording!") !=
                        PermissionStatus.Granted) return;

                    _recorder = audioService.CreateAudioRecorder();
                    _recorder.BufferMilliseconds = 20;
                    _recorder.WaveFormat = new WaveFormat(48000, 1);
                    _recorder.SelectedDevice =
                        AudioSettings.InputDevice == "Default" ? null : AudioSettings.InputDevice;
                    _recorder.DataAvailable += (_, e) =>
                    {
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
                    };

                    _recorder.StartRecording();
                    IsRecording = true;
                }
            }
            catch (Exception ex)
            {
                notificationService.SendErrorNotification(ex.Message);
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
                    _player.Stop();
                    _player.Dispose();
                    _player = null;
                    IsPlaying = false;
                }
                else
                {
                    _player = audioService.CreateAudioPlayer();
                    _player.SelectedDevice =
                        AudioSettings.OutputDevice == "Default" ? null : AudioSettings.OutputDevice;
                    _player.Init(_signal);
                    _player.Play();
                    IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                _player?.Dispose();
                _player = null;
                notificationService.SendErrorNotification(ex.Message);
                IsPlaying = false;
            }
        }

        public override void OnAppearing()
        {
            base.OnAppearing();
            AudioSettings.ReloadAvailableDevices();
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