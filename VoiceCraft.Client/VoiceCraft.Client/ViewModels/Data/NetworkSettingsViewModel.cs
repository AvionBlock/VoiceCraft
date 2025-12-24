using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class NetworkSettingsViewModel : ObservableObject, IDisposable
{
    private readonly NetworkSettings _networkSettings;
    private readonly SettingsService _settingsService;
    private bool _disposed;

    [ObservableProperty] private PositioningType _positioningType;
    [ObservableProperty] private string _mcWssListenIp;
    [ObservableProperty] private ushort _mcWssHostPort;
    private bool _updating;

    public NetworkSettingsViewModel(SettingsService settingsService)
    {
        _networkSettings = settingsService.NetworkSettings;
        _settingsService = settingsService;
        _networkSettings.OnUpdated += Update;
        _positioningType = _networkSettings.PositioningType;
        _mcWssListenIp = _networkSettings.McWssListenIp;
        _mcWssHostPort = _networkSettings.McWssHostPort;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _networkSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnPositioningTypeChanged(PositioningType value)
    {
        ThrowIfDisposed();
        
        if (_updating) return;
        _updating = true;
        _networkSettings.PositioningType = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnMcWssListenIpChanged(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _networkSettings.McWssListenIp = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnMcWssHostPortChanging(ushort value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _networkSettings.McWssHostPort = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(NetworkSettings networkSettings)
    {
        if (_updating) return;
        _updating = true;

        PositioningType = networkSettings.PositioningType;
        McWssListenIp = networkSettings.McWssListenIp;
        McWssHostPort = networkSettings.McWssHostPort;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(ServerViewModel).ToString());
    }
}