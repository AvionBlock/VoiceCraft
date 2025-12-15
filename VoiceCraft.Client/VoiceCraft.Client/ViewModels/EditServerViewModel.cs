using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels;

public partial class EditServerViewModel(
    NavigationService navigationService,
    NotificationService notificationService,
    SettingsService settings)
    : ViewModelBase
{
    [ObservableProperty] private Server _editableServer = new();
    [ObservableProperty] private Server _server = new();

    public override void OnAppearing(object? data = null)
    {
        if (data is not EditServerNavigationData navigationData) return;
        Server = navigationData.Server;
        EditableServer = (Server)navigationData.Server.Clone();
    }

    [RelayCommand]
    private void Cancel()
    {
        navigationService.Back();
    }

    [RelayCommand]
    private void EditServer()
    {
        try
        {
            Server.Name = EditableServer.Name;
            Server.Ip = EditableServer.Ip;
            Server.Port = EditableServer.Port;
            
            notificationService.SendNotification(Localizer.Get($"Notification.Servers.Edited:{Server.Name}"),
                Localizer.Get("Notification.Servers.Badge"));
            EditableServer = new Server();
            _ = settings.SaveAsync();
            navigationService.Back();
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(ex.Message);
        }
    }
}