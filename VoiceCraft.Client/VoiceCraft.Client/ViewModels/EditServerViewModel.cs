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

    [ObservableProperty] public partial ServerSettings EditableServerSettings { get; set; } = new();
    [ObservableProperty] public partial decimal? EditableServerPort { get; set; } = 9050;
    [ObservableProperty] public partial ServerSettings ServerSettings { get; set; } = new();

    public override void OnAppearing(object? data = null)
    {
        if (data is not EditServerNavigationData navigationData) return;
        ServerSettings = navigationData.ServerSettings;
        EditableServerSettings = (ServerSettings)navigationData.ServerSettings.Clone();
        _updatingPort = true;
        EditableServerPort = EditableServerSettings.Port;
        _updatingPort = false;
    }

    partial void OnEditableServerPortChanged(decimal? value)
    {
        if (_updatingPort) return;
        if (value == null) return;
        var clamped = Math.Clamp(decimal.ToInt32(decimal.Round(value.Value)), 1, 65535);
        _updatingPort = true;
        EditableServerSettings.Port = (ushort)clamped;
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

            ServerSettings.Name = EditableServerSettings.Name;
            ServerSettings.Ip = EditableServerSettings.Ip;
            ServerSettings.Port = EditableServerSettings.Port;

            notificationService.SendNotification(
                "EditServer.Notification.Badge",
                $"EditServer.Notification.Edited:{ServerSettings.Name}");
            EditableServerSettings = new ServerSettings();
            _ = settings.SaveAsync();
            navigationService.Back();
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification("EditServer.Notification.Badge", ex.Message);
        }
    }
}