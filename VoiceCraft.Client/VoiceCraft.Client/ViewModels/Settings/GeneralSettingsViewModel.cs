using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class GeneralSettingsViewModel(
        NavigationService navigationService,
        ThemesService themesService,
        SettingsService settingsService)
        : ViewModelBase, IDisposable
    {
        //Language Settings
        [ObservableProperty] private LocaleSettingsViewModel _localeSettings = new(settingsService);

        //Theme Settings
        [ObservableProperty] private ObservableCollection<string> _locales = new(Localizer.Languages);
        [ObservableProperty] private ObservableCollection<RegisteredTheme> _themes = new(themesService.RegisteredThemes);
        [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages = new(themesService.RegisteredBackgroundImages);
        [ObservableProperty] private ThemeSettingsViewModel _themeSettings = new(settingsService, themesService);

        //Notification Settings
        [ObservableProperty] private NotificationSettingsViewModel _notificationSettings = new(settingsService);

        //Server Settings
        [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);

        [RelayCommand]
        private void Cancel()
        {
            if (DisableBackButton) return;
            navigationService.Back();
        }
        
        public void Dispose()
        {
            ThemeSettings.Dispose();
            NotificationSettings.Dispose();
            ServersSettings.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}