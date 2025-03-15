using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Models.Settings;
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
        private readonly BackgroundService _backgroundService;
        private readonly NotificationService _notificationService;
        private readonly AudioService _audioService;

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
        }

        public override void OnAppearing()
        {
        }

        public override void OnDisappearing()
        {
        }

        public void Dispose()
        {
            SelectedServer?.Dispose();
            ServersSettings.Dispose();
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
    }
}