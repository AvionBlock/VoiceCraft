using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels;

public partial class EditServerViewModel(
    NavigationService navigationService,
    NotificationService notificationService,
    SettingsService settings)
    : ViewModelBase
{
    private bool _updatingPort;

    [ObservableProperty] private Server _editableServer = new();
    [ObservableProperty] private decimal? _editableServerPort = 9050;
    [ObservableProperty] private Server _server = new();

    public override void OnAppearing(object? data = null)
    {
        if (data is not EditServerNavigationData navigationData) return;
        Server = navigationData.Server;
        EditableServer = (Server)navigationData.Server.Clone();
        _updatingPort = true;
        EditableServerPort = EditableServer.Port;
        _updatingPort = false;
    }

    partial void OnEditableServerPortChanged(decimal? value)
    {
        if (_updatingPort) return;
        if (value == null) return;
        var clamped = Math.Clamp(decimal.ToInt32(decimal.Round(value.Value)), 1, 65535);
        _updatingPort = true;
        EditableServer.Port = (ushort)clamped;
        if (EditableServerPort != clamped)
            EditableServerPort = clamped;
        _updatingPort = false;
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
            if (EditableServerPort == null)
                throw new Exception("Server port must be between 1 and 65535.");

            Server.Name = EditableServer.Name;
            Server.Ip = EditableServer.Ip;
            Server.Port = EditableServer.Port;

            notificationService.SendNotification(
                "EditServer.Notification.Badge",
                $"EditServer.Notification.Edited:{Server.Name}");
            EditableServer = new Server();
            _ = settings.SaveAsync();
            navigationService.Back();
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification("EditServer.Notification.Badge", ex.Message);
        }
    }
}