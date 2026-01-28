using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ServerDataViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;

    public readonly Server Server;
    private bool _disposed;
    [ObservableProperty] private string _ip;
    [ObservableProperty] private string _name;
    [ObservableProperty] private ushort _port;
    private bool _updating;

    public ServerDataViewModel(Server server, SettingsService settingsService)
    {
        Server = server;
        _settingsService = settingsService;
        Server.OnUpdated += Update;
        _name = Server.Name;
        _ip = Server.Ip;
        _port = Server.Port;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Server.OnUpdated -= Update;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    partial void OnNameChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        Server.Name = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnIpChanging(string value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        Server.Ip = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    partial void OnPortChanging(ushort value)
    {
        ThrowIfDisposed();

        if (_updating) return;
        _updating = true;
        Server.Port = value;
        _ = _settingsService.SaveAsync();
        _updating = false;
    }

    private void Update(Server server)
    {
        if (_updating) return;
        _updating = true;

        Name = server.Name;
        Ip = server.Ip;
        Port = server.Port;

        _updating = false;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(ServerDataViewModel).ToString());
    }
}