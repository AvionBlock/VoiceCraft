using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class AddServerViewModel(NotificationService notificationService, SettingsService settings, NavigationService navigationService) : ViewModelBase
    {
        [ObservableProperty]
        private ServersSettings _servers = settings.ServersSettings;

        [ObservableProperty]
        private Server _server = new();
        
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

                notificationService.SendSuccessNotification($"{Server.Name} has been added.");
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
}