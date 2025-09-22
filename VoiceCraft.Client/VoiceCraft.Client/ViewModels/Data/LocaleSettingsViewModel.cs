using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class LocaleSettingsViewModel : ObservableObject, IDisposable
{
    private readonly LocaleSettings _localeSettings;
    private readonly SettingsService _settingsService;

    [ObservableProperty] private string _culture;
    private bool _disposed;
    private bool _updating;

    public LocaleSettingsViewModel(SettingsService settingsService)
    {
        _localeSettings = settingsService.LocaleSettings;
        _settingsService = settingsService;
        _localeSettings.OnUpdated += Update;
        _culture = _localeSettings.Culture;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _localeSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnCultureChanging(string? value)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(value)) return; //fsr this happens.
        Localizer.Instance.Language = value;

        if (_updating) return;
        _updating = true;
        _localeSettings.Culture = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(LocaleSettings localeSettings)
    {
        if (_updating) return;
        _updating = true;

        Culture = localeSettings.Culture;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(LocaleSettingsViewModel).ToString());
    }
}