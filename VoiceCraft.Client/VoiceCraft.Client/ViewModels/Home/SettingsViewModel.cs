using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class SettingsViewModel(ThemesService themesService, SettingsService settingsService, AudioService audioService) : ViewModelBase, IDisposable
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
        [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages = new (themesService.RegisteredBackgroundImages);
        [ObservableProperty] private ThemeSettingsViewModel _themeSettings = new(settingsService.Get<ThemeSettings>(), settingsService, themesService);
        //Notification Settings
        [ObservableProperty] private NotificationSettingsViewModel _notificationSettings = new(settingsService.Get<NotificationSettings>(), settingsService);
        //Server Settings
        [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService.Get<ServersSettings>(), settingsService);
        
        //Audio Settings
        [ObservableProperty] private bool _audioSettingsExpanded;
        [ObservableProperty] private AudioSettingsViewModel _audioSettings = new(settingsService.Get<AudioSettings>(), settingsService);
        [ObservableProperty] private ObservableCollection<string> _inputDevices = new(audioService.GetInputDevices());
        [ObservableProperty] private ObservableCollection<string> _outputDevices = new(audioService.GetOutputDevices());
        [ObservableProperty] private ObservableCollection<RegisteredPreprocessor> _preprocessors = new(audioService.RegisteredPreprocessors);
        [ObservableProperty] private ObservableCollection<RegisteredEchoCanceler> _echoCancelers = new(audioService.RegisteredEchoCancelers);
        
        //Testers
        [ObservableProperty] private bool _isRecording;
        [ObservableProperty] private bool _isPlaying;
        [ObservableProperty] private int _microphoneValue;

        [RelayCommand]
        private void TestRecorder()
        {
            
        }
        
        [RelayCommand]
        private void TestPlayer()
        {
            
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