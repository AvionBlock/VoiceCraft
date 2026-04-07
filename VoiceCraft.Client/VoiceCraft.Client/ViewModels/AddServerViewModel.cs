using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels;

public partial class AddServerViewModel(
    NotificationService notificationService,
    SettingsService settings,
    NavigationService navigationService) : ViewModelBase
{
    private bool _updatingPort;

    [ObservableProperty] private Server _server = new();
    [ObservableProperty] private decimal? _serverPort = 9050;

    [ObservableProperty] private ServersSettings _servers = settings.ServersSettings;

    partial void OnServerChanged(Server value)
    {
        _updatingPort = true;
        ServerPort = value.Port;
        _updatingPort = false;
    }

    partial void OnServerPortChanged(decimal? value)
    {
        if (_updatingPort) return;
        if (value == null) return;
        var clamped = Math.Clamp(decimal.ToInt32(decimal.Round(value.Value)), 1, 65535);
        _updatingPort = true;
        Server.Port = (ushort)clamped;
        if (ServerPort != clamped)
            ServerPort = clamped;
        _updatingPort = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        navigationService.Back();
    }

    [RelayCommand]
    private void AddServer()
    {
        try
        {
            Servers.AddServer(Server);
            notificationService.SendSuccessNotification(
                "AddServer.Notification.Badge",
                $"AddServer.Notification.Added:{Server.Name}");
            Server = new Server();
            _ = settings.SaveAsync();
            navigationService.Back();
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification("AddServer.Notification.Badge", ex.Message);
        }
    }
}
