using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ServerViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly Server _server;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private string _name;
        [ObservableProperty] private string _ip;
        [ObservableProperty] private ushort _port;

        public ServerViewModel(Server server, SettingsService settingsService)
        {
            _server = server;
            _settingsService = settingsService;
            _server.OnUpdated += Update;
            _name = _server.Name;
            _ip = _server.Ip;
            _port = _server.Port;
        }

        partial void OnNameChanging(string value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _server.Name = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnIpChanging(string value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _server.Ip = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnPortChanging(ushort value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _server.Port = value;
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
            throw new ObjectDisposedException(nameof(ServerViewModel));
        }

        public void Dispose()
        {
            if(_disposed) return;
            _server.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}