using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

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
            if (ServerPort == null)
                throw new Exception(Localizer.Get("Validation.Server.PortRange"));

            Servers.AddServer(Server);
            notificationService.SendSuccessNotification(Localizer.Get($"Notification.Servers.Added:{Server.Name}"),
                Localizer.Get("Notification.Servers.Badge"));
            Server = new Server();
            _ = settings.SaveAsync();
            navigationService.Back();
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(ex.Message);
        }
    }
}
