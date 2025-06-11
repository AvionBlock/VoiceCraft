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
    private Task? _pinger;

    [ObservableProperty] private ServerViewModel? _selectedServer;

    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);

    [ObservableProperty] private string _latency = string.Empty;
    [ObservableProperty] private string _motd = string.Empty;
    [ObservableProperty] private string _positioningType = string.Empty;
    [ObservableProperty] private string _connectedClients = string.Empty;
    private bool _stopPinger;

    public void Dispose()
    {
        SelectedServer?.Dispose();
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        if (data is SelectedServerNavigationData navigationData)
            SelectedServer = new ServerViewModel(navigationData.Server, settingsService);

        if (_pinger != null)
            _stopPinger = true;
        while (_pinger is { IsCompleted: false }) Task.Delay(10).Wait(); //Don't burn the CPU!.

        _stopPinger = false;
        Latency = Locales.Locales.SelectedServer_ServerInfo_Status_Pinging;
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
            notificationService.SendNotification("Background worker failed to start VOIP process!"); //TODO NEED TO LOCALE THESE!
            _ = backgroundService.StopBackgroundProcess<VoipBackgroundProcess>(); //Don't care if it fails.
        }

        DisableBackButton = false;
    }

    [RelayCommand]
    private void EditServer()
    {
        if (SelectedServer == null) return;
        navigationService.NavigateTo<EditServerViewModel>(new EditServerNavigationData(SelectedServer.Server));
    }
    
    [RelayCommand]
    private void DeleteServer()
    {
        if (SelectedServer == null) return;
        ServersSettings.ServersSettings.RemoveServer(SelectedServer.Server);
        notificationService.SendSuccessNotification($"{SelectedServer.Name} has been removed."); //TODO NEED TO LOCALE THESE!
        _ = settingsService.SaveAsync();
        navigationService.Back();
    }

    private void OnServerInfo(ServerInfo info)
    {
        Latency = Locales.Locales.SelectedServer_ServerInfo_Status_Latency.Replace("{latency}", (Environment.TickCount - info.Tick).ToString());
        Motd = Locales.Locales.SelectedServer_ServerInfo_Status_Motd.Replace("{motd}", info.Motd);
        PositioningType = Locales.Locales.SelectedServer_ServerInfo_Status_PositioningType.Replace("{positioningType}", info.PositioningType.ToString());
        ConnectedClients = Locales.Locales.SelectedServer_ServerInfo_Status_ConnectedClients.Replace("{connectedClients}", info.Clients.ToString());
    }
}