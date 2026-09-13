using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ServerDataViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;

    public readonly ServerSettings ServerSettings;
    private bool _disposed;
    [ObservableProperty] public partial string Ip { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial ushort Port { get; set; }

    private bool _updating;

    public ServerDataViewModel(ServerSettings serverSettings, SettingsService settingsService)
    {
        ServerSettings = serverSettings;
        _settingsService = settingsService;
        ServerSettings.OnUpdated += Update;
        Name = ServerSettings.Name;
        Ip = ServerSettings.Ip;
        Port = ServerSettings.Port;
    }

    public void Dispose()
    {
        if (_disposed) return;
        ServerSettings.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnNameChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        ServerSettings.Name = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnIpChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        ServerSettings.Ip = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnPortChanging(ushort value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        ServerSettings.Port = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(ServerSettings serverSettings)
    {
        if (_updating) return;
        _updating = true;

        Name = serverSettings.Name;
        Ip = serverSettings.Ip;
        Port = serverSettings.Port;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(ServerDataViewModel).ToString());
    }
}