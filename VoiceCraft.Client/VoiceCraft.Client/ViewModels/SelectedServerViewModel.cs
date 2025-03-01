using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SelectedServerViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationService _navigationService;
        private readonly SettingsService _settingsService;
        private readonly VoiceCraftClient _voiceCraftClient;
        private readonly BackgroundService _backgroundService;
        private readonly NotificationService _notificationService;
        private readonly AudioService _audioService;
        private CancellationTokenSource? _clientPingCancellation;

        [ObservableProperty] private ServersSettingsViewModel _serversSettings;

        [ObservableProperty] private ServerViewModel? _selectedServer;

        [ObservableProperty] private string _statusInfo = string.Empty;

        [ObservableProperty] private int _latency;

        public SelectedServerViewModel(NavigationService navigationService, SettingsService settingsService, BackgroundService backgroundService,
            NotificationService notificationService, AudioService audioService)
        {
            _navigationService = navigationService;
            _settingsService = settingsService;
            _backgroundService = backgroundService;
            _notificationService = notificationService;
            _audioService = audioService;
            _serversSettings = new ServersSettingsViewModel(_settingsService.Get<ServersSettings>(), _settingsService);
            _voiceCraftClient = new VoiceCraftClient();
            
            _voiceCraftClient.OnInfoReceived += OnInfoReceived;
            _voiceCraftClient.OnDisconnected += OnDisconnected;
        }

        public void SetServer(Server server)
        {
            SelectedServer?.Dispose();
            SelectedServer = new ServerViewModel(server, _settingsService);
            StartPinger();
        }

        public override void OnAppearing()
        {
            StartPinger();
        }

        public override void OnDisappearing()
        {
            if (_clientPingCancellation == null) return;
            if (_voiceCraftClient.ConnectionStatus != ConnectionStatus.Disconnected)
                _voiceCraftClient.Disconnect();
            _clientPingCancellation.Cancel();
            _clientPingCancellation.Dispose();
            _clientPingCancellation = null;
        }

        public void Dispose()
        {
            SelectedServer?.Dispose();
            ServersSettings.Dispose();
            _voiceCraftClient.Dispose();
            _clientPingCancellation?.Cancel();
            _clientPingCancellation?.Dispose();
            _voiceCraftClient.OnDisconnected -= OnDisconnected;
            GC.SuppressFinalize(this);
        }

        [RelayCommand]
        private void Cancel()
        {
            _navigationService.Back();
        }

        [RelayCommand]
        private void Connect()
        {
            if (SelectedServer == null) return;
            var process = new VoipBackgroundProcess(SelectedServer.Ip, SelectedServer.Port, _notificationService, _audioService);
            _backgroundService.StartBackgroundProcess(process)
                .ContinueWith(success =>
                {
                    if (success.Result == false)
                    {
                        Dispatcher.UIThread.Invoke(() => _notificationService.SendNotification("Background worker failed to start VOIP process!"));
                        return;
                    }

                    _navigationService.NavigateTo<VoiceViewModel>().AttachToProcess(process);
                });
        }
        
        private void OnInfoReceived(string motd, uint clients, bool discovery, PositioningType positioningType)
        {
            StatusInfo = $"{motd}\nConnected Clients: {clients}\nDiscovery: {discovery}\nPositioning Type: {positioningType}";
        }

        private void OnDisconnected(DisconnectInfo disconnectInfo)
        {
            StatusInfo = $"Failed to ping server.\nReason: {disconnectInfo.Reason}"; //We'll have to do more disconnection testing but this works.
        }

        private void StartPinger()
        {
            if (_voiceCraftClient.ConnectionStatus != ConnectionStatus.Disconnected)
                _voiceCraftClient.Disconnect();

            if (SelectedServer == null) return;
            _voiceCraftClient.Connect(SelectedServer.Ip, SelectedServer.Port, LoginType.Pinger);
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
                    Latency = _voiceCraftClient.Latency;
                    await Task.Delay(50);
                }
                
                _clientPingCancellation.Dispose();
                _clientPingCancellation = null;
            }, _clientPingCancellation.Token);
        }
    }
}