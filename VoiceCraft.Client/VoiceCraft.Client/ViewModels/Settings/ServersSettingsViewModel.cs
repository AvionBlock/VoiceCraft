using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ServersSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly ServersSettings _serversSettings;

        [ObservableProperty] private bool _hideServerAddresses;
        [ObservableProperty] private ObservableCollection<ServerViewModel> _servers;

        public ServersSettingsViewModel(ServersSettings serversSettings)
        {
            _serversSettings = serversSettings;
            _serversSettings.OnUpdated += Update;
            _hideServerAddresses = _serversSettings.HideServerAddresses;
            _servers = new ObservableCollection<ServerViewModel>(_serversSettings.Servers.Select(s => new ServerViewModel(s)));
        }

        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            
            _serversSettings.HideServerAddresses = HideServerAddresses;
            
            base.OnPropertyChanging(e);
            _updating = false;
        }
        
        private void Update(ServersSettings serversSettings)
        {
            if (_updating) return;
            _updating = true;
            
            HideServerAddresses = serversSettings.HideServerAddresses;
            Servers = new ObservableCollection<ServerViewModel>(serversSettings.Servers.Select(x => new ServerViewModel(x)));
            
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
            _serversSettings.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}