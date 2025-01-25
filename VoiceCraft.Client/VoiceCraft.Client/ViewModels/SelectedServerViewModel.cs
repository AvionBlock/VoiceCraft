using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;
using VoiceCraft.Core.Network;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SelectedServerViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationService _navigationService;
        private readonly SettingsService _settingsService;
        private readonly VoiceCraftClient _voiceCraftClient;
        private CancellationTokenSource? _clientPingCancellation;
        
        [ObservableProperty] private ServersSettingsViewModel _serversSettings;

        [ObservableProperty] private ServerViewModel _selectedServer;

        [ObservableProperty] private string _statusInfo = string.Empty;

        [ObservableProperty] private int _latency;

        public SelectedServerViewModel(NavigationService navigationService, SettingsService settingsService)
        {
            _navigationService = navigationService;
            _settingsService = settingsService;
            _serversSettings = new ServersSettingsViewModel(_settingsService.Get<ServersSettings>(), _settingsService);
            _selectedServer = new ServerViewModel(new Server(), _settingsService);
            _voiceCraftClient = new VoiceCraftClient();
            
            _voiceCraftClient.OnLatencyUpdated += OnLatencyUpdated;
            _voiceCraftClient.OnServerInfoPacketReceived += OnServerInfoPacketReceived;
            _voiceCraftClient.OnDisconnected += OnDisconnected;
        }

        public void SetServer(Server server)
        {
            SelectedServer.Dispose();
            SelectedServer = new ServerViewModel(server, _settingsService);
            
            if(_voiceCraftClient.Status != ConnectionStatus.Disconnected)
                _voiceCraftClient.Disconnect();
            _voiceCraftClient.Connect(SelectedServer.Ip, SelectedServer.Port, ConnectionType.Pinger);
            StatusInfo = "Pinging...";

            if (_clientPingCancellation != null)
            {
                _clientPingCancellation.Cancel();
                _clientPingCancellation.Dispose();
            }

            _clientPingCancellation = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_clientPingCancellation.IsCancellationRequested)
                {
                    _voiceCraftClient.Update();
                    await Task.Delay(50);
                }
                await _clientPingCancellation.CancelAsync();
                _clientPingCancellation.Dispose();
                _clientPingCancellation = null;
            }, _clientPingCancellation.Token);
        }

        [RelayCommand]
        private void Cancel()
        {
            _navigationService.Back();
            if(_voiceCraftClient.Status != ConnectionStatus.Disconnected)
                _voiceCraftClient.Disconnect();
        }

        [RelayCommand]
        private void Connect()
        {
        }
        
        private void OnLatencyUpdated(int latency)
        {
            Latency = latency;
        }
        
        private void OnServerInfoPacketReceived(ServerInfoPacket packet)
        {
            StatusInfo = $"Motd: {packet.Motd}\nConnected Players: {packet.ConnectedPlayers}\nAllows Discovery: {packet.DiscoveryEnabled}";
        }
        
        private void OnDisconnected(DisconnectInfo disconnectInfo)
        {
            StatusInfo = $"Failed to ping server.\nReason: {disconnectInfo.Reason}"; //We'll have to do more disconnection testing but this works.
        }
        
        public void Dispose()
        {
            SelectedServer.Dispose();
            ServersSettings.Dispose();
            _voiceCraftClient.Dispose();
            _voiceCraftClient.OnDisconnected -= OnDisconnected;
            GC.SuppressFinalize(this);
        }
    }
}