using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class AppearanceSettingsViewModel(
    NavigationService navigationService,
    ThemesService themesService,
    SettingsService settingsService) : ViewModelBase, IDisposable
{
    [ObservableProperty]
    public partial ObservableCollection<RegisteredBackgroundImage> BackgroundImages { get; set; } =
        new(themesService.RegisteredBackgroundImages);

    [ObservableProperty]
    public partial ObservableCollection<RegisteredTheme> Themes { get; set; } = new(themesService.RegisteredThemes);

    [ObservableProperty]
    public partial ThemeSettingsDataViewModel ThemeSettingsData { get; set; } = new(settingsService, themesService);

    public void Dispose()
    {
        ThemeSettingsData.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}