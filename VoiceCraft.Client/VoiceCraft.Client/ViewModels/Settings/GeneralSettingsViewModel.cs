using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class GeneralSettingsViewModel
    : ViewModelBase, IDisposable
{
    private readonly NavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<KeyValuePair<string, string>> _locales = [];

    //Language Settings
    [ObservableProperty] private LocaleSettingsViewModel _localeSettings;

    //Notification Settings
    [ObservableProperty] private NotificationSettingsViewModel _notificationSettings;

    //Server Settings
    [ObservableProperty] private ServersSettingsViewModel _serversSettings;

    public GeneralSettingsViewModel(NavigationService navigationService, SettingsService settingsService)
    {
        foreach (var locale in Localizer.Languages)
            _locales.Add(new KeyValuePair<string, string>(CultureInfo.GetCultureInfo(locale).NativeName, locale));
        _navigationService = navigationService;
        _localeSettings = new LocaleSettingsViewModel(settingsService);
        _notificationSettings = new NotificationSettingsViewModel(settingsService);
        _serversSettings = new ServersSettingsViewModel(settingsService);
    }

    public void Dispose()
    {
        NotificationSettings.Dispose();
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        _navigationService.Back();
    }
}