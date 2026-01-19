using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network.Clients;

namespace VoiceCraft.Client.ViewModels;

public partial class SelectedServerViewModel(
    NavigationService navigationService,
    SettingsService settingsService,
    VoiceCraftClient client,
    NotificationService notificationService)
    : ViewModelBase, IDisposable
{
    private CancellationTokenSource? _cts;
    [ObservableProperty] private string _connectedClients = string.Empty;
    [ObservableProperty] private string _latency = string.Empty;
    [ObservableProperty] private string _motd = string.Empty;
    [ObservableProperty] private string _positioningType = string.Empty;
    [ObservableProperty] private ServerViewModel? _selectedServer;
    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settingsService);
    [ObservableProperty] private string _version = string.Empty;

    public void Dispose()
    {
        SelectedServer?.Dispose();
        ServersSettings.Dispose();
        client.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        if (data is SelectedServerNavigationData navigationData)
            SelectedServer = new ServerViewModel(navigationData.Server, settingsService);

        Latency = Localizer.Get("SelectedServer.ServerInfo.Status.Pinging");
        Motd = string.Empty;
        PositioningType = string.Empty;
        ConnectedClients = string.Empty;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();
        Task.Run(() => PingerLogic(_cts.Token), _cts.Token);
    }

    public override void OnDisappearing()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
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
        notificationService.SendSuccessNotification(
            Localizer.Get($"Notification.Servers.Removed:{SelectedServer.Name}"),
            Localizer.Get("Notification.Servers.Badge"));
        _ = settingsService.SaveAsync();
        navigationService.Back();
    }

    private async Task PingerLogic(CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                client.Update();
                await Task.Delay(Constants.TickRate, token);
            }
        }, token);
        
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (SelectedServer != null)
                {
                    var result = await client.PingAsync(SelectedServer.Ip, SelectedServer.Port, token);
                    Latency = Localizer.Get(
                        $"SelectedServer.ServerInfo.Status.Latency:{Math.Max(Environment.TickCount - result.Tick - Constants.TickRate, 0)}");
                    Motd = Localizer.Get($"SelectedServer.ServerInfo.Status.Motd:{result.Motd}");
                    PositioningType =
                        Localizer.Get($"SelectedServer.ServerInfo.Status.PositioningType:{result.PositioningType}");
                    ConnectedClients =
                        Localizer.Get($"SelectedServer.ServerInfo.Status.ConnectedClients:{result.Clients}");
                    Version = Localizer.Get($"SelectedServer.ServerInfo.Status.Version:{result.Version}");
                }
            }
            catch(Exception ex)
            {
                Latency = Localizer.Get("SelectedServer.ServerInfo.Status.Pinging");
                Motd = "";
                PositioningType = "";
                ConnectedClients = "";
                Version = "";
            }
            await Task.Delay(Constants.TickRate, token);

        }
    }
}