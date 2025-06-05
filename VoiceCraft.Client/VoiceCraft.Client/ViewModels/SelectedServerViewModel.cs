using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Data;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels;

public partial class SelectedServerViewModel(
    NavigationService navigationService,
    SettingsService settingsService,
    BackgroundService backgroundService,
    NotificationService notificationService,
    AudioService audioService)
    : ViewModelBase, IDisposable
{
    [ObservableProperty] private int _latency = -1;
    private Task? _pinger;

    [ObservableProperty] private ServerViewModel? _selectedServer;

    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);

    [ObservableProperty] private string _statusInfo = string.Empty;
    private bool _stopPinger;

    public void Dispose()
    {
        SelectedServer?.Dispose();
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        if(data is SelectedServerNavigationData navigationData)
            SelectedServer = new ServerViewModel(navigationData.Server, settingsService);
        
        if (_pinger != null)
            _stopPinger = true;
        while (_pinger is { IsCompleted: false }) Task.Delay(10).Wait(); //Don't burn the CPU!.

        _stopPinger = false;
        StatusInfo = Locales.Locales.SelectedServer_ServerInfo_Status_Pinging;
        _pinger = Task.Run(async () =>
        {
            var client = new VoiceCraftClient();
            client.NetworkSystem.OnServerInfo += OnServerInfo;
            var startTime = DateTime.UtcNow;
            while (!_stopPinger)
            {
                await Task.Delay(2);
                if (SelectedServer == null) continue;
                client.Update();

                if ((DateTime.UtcNow - startTime).TotalMilliseconds < 2000) continue;
                client.Ping(SelectedServer.Ip, SelectedServer.Port);
                startTime = DateTime.UtcNow;
            }

            client.NetworkSystem.OnServerInfo -= OnServerInfo;
            client.Dispose();
        });
    }

    public override void OnDisappearing()
    {
        _stopPinger = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }

    [RelayCommand]
    private async Task Connect()
    {
        if (SelectedServer == null) return;
        var process = new VoipBackgroundProcess(SelectedServer.Ip, SelectedServer.Port, Localizer.Language, notificationService, audioService, settingsService);
        try
        {
            DisableBackButton = true;
            await backgroundService.StopBackgroundProcess<VoipBackgroundProcess>();
            await backgroundService.StartBackgroundProcess(process);
            navigationService.NavigateTo<VoiceViewModel>(new VoiceNavigationData(process));
        }
        catch
        {
            notificationService.SendNotification("Background worker failed to start VOIP process!");
            _ = backgroundService.StopBackgroundProcess<VoipBackgroundProcess>(); //Don't care if it fails.
        }

        DisableBackButton = false;
    }

    private void OnServerInfo(ServerInfo info)
    {
        var statusInfo = Locales.Locales.SelectedServer_ServerInfo_Status_Status
            .Replace("{motd}", info.Motd)
            .Replace("{positioningType}", info.PositioningType.ToString())
            .Replace("{clients}", info.Clients.ToString());
        StatusInfo = statusInfo;
        Latency = Environment.TickCount - info.Tick;
    }
}