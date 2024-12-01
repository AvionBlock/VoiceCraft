using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel(ThemesService themesService, SettingsService settingsService) : ViewModelBase
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
        [ObservableProperty] private ThemeSettings _themeSettings = settingsService.Get<ThemeSettings>();
        //Notification Settings
        [ObservableProperty] private NotificationSettings _notificationSettings = settingsService.Get<NotificationSettings>();
        //Server Settings
        [ObservableProperty] private ServersSettings _serversSettings = settingsService.Get<ServersSettings>();
        
        //Audio Settings
        [ObservableProperty] private bool _audioSettingsExpanded;
        [ObservableProperty] private AudioSettings _audioSettings = settingsService.Get<AudioSettings>();
        [ObservableProperty] private ObservableCollection<string> _inputDevices = new();
        [ObservableProperty] private ObservableCollection<string> _outputDevices = new();
        [ObservableProperty] private ObservableCollection<RegisteredPreprocessor> _preprocessors = new();
        [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = new();
        
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
}