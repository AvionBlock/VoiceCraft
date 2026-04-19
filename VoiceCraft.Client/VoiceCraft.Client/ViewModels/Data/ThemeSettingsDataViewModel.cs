using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ThemeSettingsDataViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly ThemeSettings _themeSettings;
    private readonly ThemesService _themesService;
    private bool _disposed;
    [ObservableProperty] public partial Guid SelectedBackgroundImage { get; set; }
    [ObservableProperty] public partial Guid SelectedTheme { get; set; }

    private bool _updating;

    public ThemeSettingsDataViewModel(SettingsService settingsService, ThemesService themesService)
    {
        _themeSettings = settingsService.ThemeSettings;
        _settingsService = settingsService;
        _themesService = themesService;
        _themeSettings.OnUpdated += Update;
        SelectedTheme = _themeSettings.SelectedTheme;
        SelectedBackgroundImage = _themeSettings.SelectedBackgroundImage;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _themeSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnSelectedThemeChanging(Guid value)
    {
        ThrowIfDisposed();
        _themesService.SwitchTheme(value);

        if (_updating) return;
        _updating = true;
        _themeSettings.SelectedTheme = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnSelectedBackgroundImageChanging(Guid value)
    {
        ThrowIfDisposed();
        _themesService.SwitchBackgroundImage(value);

        if (_updating) return;
        _updating = true;
        _themeSettings.SelectedBackgroundImage = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(ThemeSettings themeSettings)
    {
        if (_updating) return;
        _updating = true;

        SelectedTheme = themeSettings.SelectedTheme;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(ThemeSettingsDataViewModel).ToString());
    }
}