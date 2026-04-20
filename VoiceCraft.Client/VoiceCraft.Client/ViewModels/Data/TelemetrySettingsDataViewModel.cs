using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class TelemetrySettingsDataViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly TelemetrySettings _telemetrySettings;
    private bool _disposed;
    private bool _updating;

    [ObservableProperty] public partial bool Enabled { get; set; }

    public TelemetrySettingsDataViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _telemetrySettings = settingsService.TelemetrySettings;
        _telemetrySettings.OnUpdated += Update;
        Enabled = _telemetrySettings.Enabled;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _telemetrySettings.OnUpdated -= Update;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnEnabledChanging(bool value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _telemetrySettings.Enabled = value;
        _telemetrySettings.ConsentShown = true;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(TelemetrySettings telemetrySettings)
    {
        if (_updating) return;
        _updating = true;
        Enabled = telemetrySettings.Enabled;
        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(TelemetrySettingsDataViewModel).ToString());
    }
}
