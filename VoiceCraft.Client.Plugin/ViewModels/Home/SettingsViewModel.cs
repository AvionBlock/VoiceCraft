using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.Views.Home;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title => "Settings";
        private ThemesService _themesService;
        private SettingsService _settingsService;

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
        private float _microphoneValue;

        public SettingsViewModel(SettingsService settings, ThemesService themes, CreditsView credits, IAudioDevices audioDevices)
        {
            _settingsService = settings;
            _themesService = themes;
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

        public override void OnAppearing(object? sender)
        {
            base.OnAppearing(sender);
            ThemeSettings.PropertyChanged += UpdateTheme;
            ThemeSettings.PropertyChanged += SaveSettings;
            AudioSettings.PropertyChanged += SaveSettings;
            ServersSettings.PropertyChanged += SaveSettings;
            NotificationSettings.PropertyChanged += SaveSettings;
        }

        public override void OnDisappearing(object? sender)
        {
            base.OnDisappearing(sender);
            ThemeSettings.PropertyChanged -= UpdateTheme;
            ThemeSettings.PropertyChanged -= SaveSettings;
            AudioSettings.PropertyChanged -= SaveSettings;
            ServersSettings.PropertyChanged -= SaveSettings;
            NotificationSettings.PropertyChanged -= SaveSettings;
        }
    }

    internal class ObservableCollection : ObservableCollection<string>
    {
        public ObservableCollection(IEnumerable<string> collection) : base(collection)
        {
        }
    }
}