using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel(ThemesService themesService, SettingsService settingsService, AudioService audioService) : ViewModelBase
    {
        private SignalGenerator _signal = new(48000, 2)
        {
            Gain = 0.2,
            Frequency = 500,
            Type = SignalGeneratorType.Sin
        };

        [ObservableProperty] private bool _generalSettingsExpanded;
        //Theme Settings
        [ObservableProperty] private ObservableCollection<RegisteredTheme> _themes = new(themesService.RegisteredThemes);
        [ObservableProperty] private ThemeSettingsViewModel _themeSettings = new(settingsService.Get<ThemeSettings>());
        //Notification Settings
        [ObservableProperty] private NotificationSettingsViewModel _notificationSettings = new(settingsService.Get<NotificationSettings>());
        //Server Settings
        [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService.Get<ServersSettings>());
        
        //Audio Settings
        [ObservableProperty] private bool _audioSettingsExpanded;
        [ObservableProperty] private AudioSettingsViewModel _audioSettings = new(settingsService.Get<AudioSettings>());
        [ObservableProperty] private ObservableCollection<string> _inputDevices = new(audioService.GetInputDevices());
        [ObservableProperty] private ObservableCollection<string> _outputDevices = new(audioService.GetOutputDevices());
        [ObservableProperty] private ObservableCollection<RegisteredPreprocessor> _preprocessors = [];
        [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = [];
        
        //Testers
        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private int _microphoneValue;

        [RelayCommand]
        public void TestRecorder()
        {
            
        }
        
        [RelayCommand]
        public void TestPlayer()
        {
            
        }
    }

    public partial class AudioSettingsViewModel(AudioSettings audioSettings) : ObservableObject
    {
        [ObservableProperty] private string _inputDevice = audioSettings.InputDevice;
        [ObservableProperty] private string _outputDevice = audioSettings.OutputDevice;
        [ObservableProperty] private Guid _preprocessor = audioSettings.Preprocessor;
        [ObservableProperty] private Guid _echoCanceler = audioSettings.EchoCanceler;
        [ObservableProperty] private float _microphoneSensitivity = audioSettings.MicrophoneSensitivity;
        [ObservableProperty] private bool _AEC = audioSettings.AEC;
        [ObservableProperty] private bool _AGC = audioSettings.AGC;
        [ObservableProperty] private bool _denoiser = audioSettings.Denoiser;
        [ObservableProperty] private bool _VAD = audioSettings.VAD;

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            audioSettings.InputDevice = InputDevice;
            audioSettings.OutputDevice = OutputDevice;
            audioSettings.Preprocessor = Preprocessor;
            audioSettings.EchoCanceler = EchoCanceler;
            audioSettings.MicrophoneSensitivity = MicrophoneSensitivity;
            audioSettings.AEC = AEC;
            audioSettings.AGC = AGC;
            audioSettings.Denoiser = Denoiser;
            audioSettings.VAD = VAD;
            base.OnPropertyChanging(e);
        }
    }

    public partial class ThemeSettingsViewModel(ThemeSettings themeSettings) : ObservableObject
    {
        [ObservableProperty] private string _selectedTheme = themeSettings.SelectedTheme;

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            themeSettings.SelectedTheme = SelectedTheme;
            base.OnPropertyChanging(e);
        }
    }

    public partial class NotificationSettingsViewModel(NotificationSettings notificationSettings) : ObservableObject
    {
        [ObservableProperty] private ushort _dismissDelayMS = notificationSettings.DismissDelayMS;
        [ObservableProperty] private bool _disableNotifications = notificationSettings.DisableNotifications;
        
        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            notificationSettings.DismissDelayMS = DismissDelayMS;
            notificationSettings.DisableNotifications = DisableNotifications;
            base.OnPropertyChanging(e);
        }
    }

    public partial class ServersSettingsViewModel(ServersSettings serversSettings) : ObservableObject
    {
        [ObservableProperty] private bool _hideServerAddresses = serversSettings.HideServerAddresses;
        [ObservableProperty] private IEnumerable<Server> _servers = serversSettings.Servers;

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            serversSettings.HideServerAddresses = HideServerAddresses;
            base.OnPropertyChanging(e);
        }
    }
}