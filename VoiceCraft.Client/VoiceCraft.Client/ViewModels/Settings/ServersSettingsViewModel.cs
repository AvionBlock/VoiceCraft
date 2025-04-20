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
        private readonly SettingsService _settingsService;

        public readonly ServersSettings ServersSettings;
        [ObservableProperty] private bool _hideServerAddresses;
        [ObservableProperty] private ObservableCollection<ServerViewModel> _servers;

        public ServersSettingsViewModel(SettingsService settingsService)
        {
            ServersSettings = settingsService.ServersSettings;
            _settingsService = settingsService;
            ServersSettings.OnUpdated += Update;
            _hideServerAddresses = ServersSettings.HideServerAddresses;
            _servers = new ObservableCollection<ServerViewModel>(ServersSettings.Servers.Select(s => new ServerViewModel(s, _settingsService)));
        }

        partial void OnHideServerAddressesChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            ServersSettings.HideServerAddresses = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(ServersSettings serversSettings)
        {
            if (_updating) return;
            _updating = true;
            
            HideServerAddresses = serversSettings.HideServerAddresses;
            foreach (var server in Servers)
            {
                server.Dispose();
            }
            Servers = new ObservableCollection<ServerViewModel>(serversSettings.Servers.Select(x => new ServerViewModel(x, _settingsService)));
            
            _updating = false;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(ServersSettingsViewModel).ToString());
        }
        
        public void Dispose()
        {
            if(_disposed) return;
            ServersSettings.OnUpdated -= Update;
            foreach (var server in Servers)
            {
                server.Dispose();
            }
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}