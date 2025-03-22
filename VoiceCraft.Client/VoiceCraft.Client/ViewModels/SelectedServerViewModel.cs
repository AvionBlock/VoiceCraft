using System;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SelectedServerViewModel(
        NavigationService navigationService,
        SettingsService settingsService,
        BackgroundService backgroundService,
        NotificationService notificationService,
        AudioService audioService)
        : ViewModelBase, IDisposable
    {
        private bool _stopPinger;
        private Task? _pinger;

        [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService.Get<ServersSettings>(), settingsService);

        [ObservableProperty] private ServerViewModel? _selectedServer;

        [ObservableProperty] private string _statusInfo = string.Empty;

        [ObservableProperty] private int _latency = -1;

        public override void OnAppearing()
        {
            if (_pinger != null)
                _stopPinger = true;
            while (_pinger is { IsCompleted: false })
            {
                Task.Delay(1).Wait(); //Don't burn the CPU!.
            }
            
            _stopPinger = false;
            StatusInfo = Locales.Locales.SelectedServer_ServerInfo_Status_Pinging;
            _pinger = Task.Run(async () =>
            {
                var client = new VoiceCraftClient();
                client.NetworkSystem.OnServerInfo += OnServerInfo;
                while (!_stopPinger)
                {
                    await Task.Delay(2000);
                    if(SelectedServer == null) continue;
                    client.Update();
                    client.Ping(SelectedServer.Ip, SelectedServer.Port);
                }
                client.NetworkSystem.OnServerInfo -= OnServerInfo;
                client.Dispose();
            });
        }

        public override void OnDisappearing()
        {
            _stopPinger = true;
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
            navigationService.Back();
        }

        [RelayCommand]
        private void Connect()
        {
            if (SelectedServer == null) return;
            var process = new VoipBackgroundProcess(SelectedServer.Ip, SelectedServer.Port, notificationService, audioService);
            backgroundService.StartBackgroundProcess(process)
                .ContinueWith(success =>
                {
                    if (success.Result == false)
                    {
                        Dispatcher.UIThread.Invoke(() => notificationService.SendNotification("Background worker failed to start VOIP process!"));
                        return;
                    }

                    navigationService.NavigateTo<VoiceViewModel>().AttachToProcess(process);
                });
        }
        
        private void OnServerInfo(IPEndPoint arg1, ServerInfo info)
        {
            var statusInfo = Locales.Locales.SelectedServer_ServerInfo_Status
                .Replace("{motd}", info.Motd)
                .Replace("{discovery}", info.Discovery.ToString())
                .Replace("{positioningType}", info.PositioningType.ToString())
                .Replace("{clients}", info.Clients.ToString());
            StatusInfo = statusInfo;
        }
    }
}