using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class NetworkSettingsViewModel : ObservableObject, IDisposable
{
    private readonly NetworkSettings _networkSettings;
    private readonly SettingsService _settingsService;

    [ObservableProperty] private bool _packetCompression;
    [ObservableProperty] private ushort _mcWssHostPort;
    private bool _disposed;
    private bool _updating;

    public NetworkSettingsViewModel(SettingsService settingsService)
    {
        _networkSettings = settingsService.NetworkSettings;
        _settingsService = settingsService;
        _networkSettings.OnUpdated += Update;
        _packetCompression = _networkSettings.PacketCompression;
        _mcWssHostPort = _networkSettings.McWssHostPort;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _networkSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnPacketCompressionChanging(bool value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        _networkSettings.PacketCompression = value;
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

        PacketCompression = networkSettings.PacketCompression;
        McWssHostPort = networkSettings.McWssHostPort;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(ServerViewModel).ToString());
    }
}