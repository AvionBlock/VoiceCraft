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
    [ObservableProperty] private ServerDataViewModel? _selectedServer;

    [ObservableProperty] private ServersSettingsViewModel _serversSettings = new(settings);

    public void Dispose()
    {
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private void OpenServer(ServerDataViewModel? server)
    {
        if (server == null) return;
        navigationService.NavigateTo<SelectedServerViewModel>(new SelectedServerNavigationData(server.Server));
    }

    partial void OnSelectedServerChanged(ServerDataViewModel? value)
    {
        OpenServer(value);
        SelectedServer = null;
    }

    [RelayCommand]
    private void AddServer()
    {
        navigationService.NavigateTo<AddServerViewModel>();
    }

    [RelayCommand]
    private void DeleteServer(ServerDataViewModel serverData)
    {
        ServersSettings.ServersSettings.RemoveServer(serverData.Server);
        notificationService.SendSuccessNotification(
            "Servers.Notification.Badge",
            $"Servers.Notification.Removed:{serverData.Name}");
        _ = settings.SaveAsync();
    }

    [RelayCommand]
    private void EditServer(ServerDataViewModel? server)
    {
        if (server == null) return;
        navigationService.NavigateTo<EditServerViewModel>(new EditServerNavigationData(server.Server));
    }
}
