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
        private readonly SettingsService _settingsService;
        
        public readonly Server Server;

        [ObservableProperty] private string _name;
        [ObservableProperty] private string _ip;
        [ObservableProperty] private ushort _port;

        public ServerViewModel(Server server, SettingsService settingsService)
        {
            Server = server;
            _settingsService = settingsService;
            Server.OnUpdated += Update;
            _name = Server.Name;
            _ip = Server.Ip;
            _port = Server.Port;
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
            throw new ObjectDisposedException(nameof(ServerViewModel));
        }

        public void Dispose()
        {
            if(_disposed) return;
            Server.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}