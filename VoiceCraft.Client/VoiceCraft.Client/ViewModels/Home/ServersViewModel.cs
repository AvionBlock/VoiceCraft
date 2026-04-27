using System;
using System.Threading.Tasks;
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
    [ObservableProperty] public partial ServerDataViewModel? SelectedServer { get; set; }
    [ObservableProperty] public partial ServersSettingsViewModel ServersSettings { get; set; } = new(settings);

    public void Dispose()
    {
        ServersSettings.Dispose();
        GC.SuppressFinalize(this);
    }

    partial void OnSelectedServerChanged(ServerDataViewModel? value)
    {
        if (value == null) return;
        navigationService.NavigateTo<SelectedServerViewModel>(new SelectedServerNavigationData(value.Server));
        Task.Run(() =>
        {
            SelectedServer = null; //This bug is annoying.
        });
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