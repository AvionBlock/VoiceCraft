using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
    private bool _disposed;

    [ObservableProperty] public partial ObservableCollection<CultureDataViewModel> Locales { get; set; } = [];

    //Language Settings
    [ObservableProperty] public partial LocaleSettingsDataViewModel LocaleSettingsData { get; set; }

    //Notification Settings
    [ObservableProperty] public partial NotificationSettingsDataViewModel NotificationSettingsData { get; set; }

    //Telemetry Settings
    [ObservableProperty] public partial TelemetrySettingsDataViewModel TelemetrySettingsData { get; set; }

    //Server Settings
    [ObservableProperty] public partial ServersSettingsViewModel ServersSettings { get; set; }

    public GeneralSettingsViewModel(NavigationService navigationService, SettingsService settingsService)
    {
        foreach (var locale in Localizer.Languages)
            Locales.Add(new CultureDataViewModel(CultureInfo.GetCultureInfo(locale).NativeName,
                locale,
                LoadImage($"avares://VoiceCraft.Client/Assets/Flags/{locale}.png")));
        _navigationService = navigationService;
        LocaleSettingsData = new LocaleSettingsDataViewModel(settingsService);
        TelemetrySettingsData = new TelemetrySettingsDataViewModel(settingsService);
        NotificationSettingsData = new NotificationSettingsDataViewModel(settingsService);
        ServersSettings = new ServersSettingsViewModel(settingsService);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var locale in Locales)
            locale.Dispose();
        Locales.Clear();
        NotificationSettingsData.Dispose();
        TelemetrySettingsData.Dispose();
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

    private static Bitmap? LoadImage(string path)
    {
        var uri = new Uri(path);
        if (!AssetLoader.Exists(uri)) return null;
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }
}
