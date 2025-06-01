using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class GeneralSettingsViewModel(
    NavigationService navigationService,
    ThemesService themesService,
    SettingsService settingsService)
    : ViewModelBase, IDisposable
{
    [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages = new(themesService.RegisteredBackgroundImages);

    //Theme Settings
    [ObservableProperty] private ObservableCollection<KeyValuePair<string, string>> _locales =
    [
        new("English (US)", "en-us"),
        new("Netherlands", "nl-nl"),
        new("Chinese (PRC)", "zh-cn"),
        new("Chinese (Taiwan)", "zh-tw")
    ];

    //Language Settings
    [ObservableProperty] private LocaleSettingsViewModel _localeSettings = new(settingsService);

    //Notification Settings
    [ObservableProperty] private NotificationSettingsViewModel _notificationSettings = new(settingsService);

    //Server Settings
    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);
    [ObservableProperty] private ObservableCollection<RegisteredTheme> _themes = new(themesService.RegisteredThemes);
    [ObservableProperty] private ThemeSettingsViewModel _themeSettings = new(settingsService, themesService);

    public void Dispose()
    {
        ThemeSettings.Dispose();
        NotificationSettings.Dispose();
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}