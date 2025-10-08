using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels;

public partial class SelectedServerViewModel(
    NavigationService navigationService,
    SettingsService settingsService,
    BackgroundService backgroundService,
    NotificationService notificationService,
    AudioService audioService)
    : ViewModelBase, IDisposable
{
    [ObservableProperty] private string _connectedClients = string.Empty;

    [ObservableProperty] private string _latency = string.Empty;
    [ObservableProperty] private string _motd = string.Empty;
    private Task? _pinger;
    [ObservableProperty] private string _positioningType = string.Empty;

    [ObservableProperty] private ServerViewModel? _selectedServer;

    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);
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
        Motd = string.Empty;
        PositioningType = string.Empty;
        ConnectedClients = string.Empty;

        _pinger = Task.Run(async () =>
        {
            var client = new VoiceCraftClient();
            client.OnServerInfo += OnServerInfo;
            var startTime = DateTime.MinValue;
            while (!_stopPinger)
            {
                client.Update();
                await Task.Delay(Constants.TickRate);
                if (SelectedServer == null) continue;

                if (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2)) continue;
                client.Ping(SelectedServer.Ip, SelectedServer.Port);
                startTime = DateTime.UtcNow;
            }

            client.OnServerInfo -= OnServerInfo;
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
        var process = new VoipBackgroundProcess(SelectedServer.Ip, SelectedServer.Port, Localizer.Instance.Language,
            notificationService, audioService, settingsService);
        try
        {
            DisableBackButton = true;
            await backgroundService.StopBackgroundProcess<VoipBackgroundProcess>();
            await backgroundService.StartBackgroundProcess(process);
            navigationService.NavigateTo<VoiceViewModel>(new VoiceNavigationData(process));
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                "Background worker failed to start VOIP process!"); //TODO NEED TO LOCALE THESE!
            _ = backgroundService.StopBackgroundProcess<VoipBackgroundProcess>(); //Don't care if it fails.
            LogService.Log(ex);
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
        notificationService.SendSuccessNotification($"{SelectedServer.Name} has been removed.",
            Locales.Locales.Notification_Badges_Servers); //TODO NEED TO LOCALE THESE!
        _ = settingsService.SaveAsync();
        navigationService.Back();
    }

    private void OnServerInfo(ServerInfo info)
    {
        Latency = Locales.Locales.SelectedServer_ServerInfo_Status_Latency.Replace("{latency}",
            Math.Max(Environment.TickCount - info.Tick - Constants.TickRate, 0).ToString());
        Motd = Locales.Locales.SelectedServer_ServerInfo_Status_Motd.Replace("{motd}", info.Motd);
        PositioningType =
            Locales.Locales.SelectedServer_ServerInfo_Status_PositioningType.Replace("{positioningType}",
                info.PositioningType.ToString());
        ConnectedClients =
            Locales.Locales.SelectedServer_ServerInfo_Status_ConnectedClients.Replace("{connectedClients}",
                info.Clients.ToString());
    }
}