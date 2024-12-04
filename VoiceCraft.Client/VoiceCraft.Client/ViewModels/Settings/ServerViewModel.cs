using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ServerViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly Server _server;

        [ObservableProperty] private string _name;
        [ObservableProperty] private string _ip;
        [ObservableProperty] private ushort _port;
        [ObservableProperty] private string _key;

        public ServerViewModel(Server server)
        {
            _server = server;
            _server.OnUpdated += Update;
            _name = _server.Name;
            _ip = _server.Ip;
            _port = _server.Port;
            _key = _server.Key;
        }

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            
            _server.Name = Name;
            _server.Ip = Ip;
            _server.Port = Port;
            _server.Key = Key;
            
            base.OnPropertyChanging(e);
            _updating = false;
        }

        private void Update(Server server)
        {
            if (_updating) return;
            _updating = true;
            
            Name = server.Name;
            Ip = server.Ip;
            Port = server.Port;
            Key = server.Key;
            
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