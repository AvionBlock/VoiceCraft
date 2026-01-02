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
    [ObservableProperty] private ObservableCollection<RegisteredBackgroundImage> _backgroundImages =
        new(themesService.RegisteredBackgroundImages);

    [ObservableProperty] private ObservableCollection<RegisteredTheme> _themes = new(themesService.RegisteredThemes);
    [ObservableProperty] private ThemeSettingsViewModel _themeSettings = new(settingsService, themesService);

    public void Dispose()
    {
        ThemeSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}