using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class ServersViewModel(
    NavigationService navigationService,
    NotificationService notificationService,
    SettingsService settings)
    : ViewModelBase, IDisposable
{
    [ObservableProperty] private ServerViewModel? _selectedServer;

    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settings);

    public void Dispose()
    {
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    partial void OnSelectedServerChanged(ServerViewModel? value)
    {
        if (value == null) return;
        navigationService.NavigateTo<SelectedServerViewModel>(new SelectedServerNavigationData(value.Server));
        SelectedServer = null;
    }

    [RelayCommand]
    private void AddServer()
    {
        navigationService.NavigateTo<AddServerViewModel>();
    }

    [RelayCommand]
    private void DeleteServer(ServerViewModel server)
    {
        ServersSettings.ServersSettings.RemoveServer(server.Server);
        //TODO Locale This!
        notificationService.SendSuccessNotification($"{server.Name} has been removed.",
            Locales.Locales.Notification_Badges_Servers);
        _ = settings.SaveAsync();
    }

    [RelayCommand]
    private void EditServer(ServerViewModel? server)
    {
        if (server == null) return;
        navigationService.NavigateTo<EditServerViewModel>(new EditServerNavigationData(server.Server));
    }
}