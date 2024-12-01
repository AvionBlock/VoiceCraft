using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class AddServerViewModel(NotificationService notificationService, SettingsService settings) : ViewModelBase
    {
        [ObservableProperty]
        private ServersSettings _servers = settings.Get<ServersSettings>();

        [ObservableProperty]
        private Server _server = new();

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Servers.AddServer(Server);

                notificationService.SendSuccessNotification($"{Server.Name} has been added.");
                Server = new Server();
                _ = settings.SaveAsync();
            }
            catch (Exception ex)
            {
                notificationService.SendErrorNotification(ex.Message);
            }
        }
    }
}