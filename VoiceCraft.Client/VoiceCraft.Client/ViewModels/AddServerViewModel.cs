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
    [ObservableProperty] private Server _server = new();

    [ObservableProperty] private ServersSettings _servers = settings.ServersSettings;

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