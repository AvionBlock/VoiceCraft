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

    [ObservableProperty] public partial ObservableCollection<KeyValuePair<string, string>> Locales { get; set; } = [];

    //Language Settings
    [ObservableProperty] public partial LocaleSettingsDataViewModel LocaleSettingsData { get; set; }

    //Notification Settings
    [ObservableProperty] public partial NotificationSettingsDataViewModel NotificationSettingsData { get; set; }

    //Server Settings
    [ObservableProperty] public partial ServersSettingsViewModel ServersSettings { get; set; }

    public GeneralSettingsViewModel(NavigationService navigationService, SettingsService settingsService)
    {
        foreach (var locale in Localizer.Languages)
            Locales.Add(new KeyValuePair<string, string>(CultureInfo.GetCultureInfo(locale).NativeName, locale));
        _navigationService = navigationService;
        LocaleSettingsData = new LocaleSettingsDataViewModel(settingsService);
        NotificationSettingsData = new NotificationSettingsDataViewModel(settingsService);
        ServersSettings = new ServersSettingsViewModel(settingsService);
    }

    public void Dispose()
    {
        NotificationSettingsData.Dispose();
        LocaleSettingsData.Dispose();
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