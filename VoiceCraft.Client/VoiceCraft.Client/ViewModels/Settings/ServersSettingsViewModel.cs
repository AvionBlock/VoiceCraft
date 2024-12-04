using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ServersSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly ServersSettings _serversSettings;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private bool _hideServerAddresses;
        [ObservableProperty] private ObservableCollection<ServerViewModel> _servers;

        public ServersSettingsViewModel(ServersSettings serversSettings, SettingsService settingsService)
        {
            _serversSettings = serversSettings;
            _settingsService = settingsService;
            _serversSettings.OnUpdated += Update;
            _hideServerAddresses = _serversSettings.HideServerAddresses;
            _servers = new ObservableCollection<ServerViewModel>(_serversSettings.Servers.Select(s => new ServerViewModel(s, _settingsService)));
        }

        partial void OnHideServerAddressesChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _serversSettings.HideServerAddresses = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(ServersSettings serversSettings)
        {
            if (_updating) return;
            _updating = true;
            
            HideServerAddresses = serversSettings.HideServerAddresses;
            Servers = new ObservableCollection<ServerViewModel>(serversSettings.Servers.Select(x => new ServerViewModel(x, _settingsService)));
            
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
            foreach (var server in Servers)
            {
                server.Dispose();
            }
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}