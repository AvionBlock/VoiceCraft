using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class EditServerViewModel(NavigationService navigationService, NotificationService notificationService, SettingsService settings)
        : ViewModelBase
    {
        [ObservableProperty]
        private ServersSettings _servers = settings.Get<ServersSettings>();
        [ObservableProperty]
        private Server _server = new();

        [ObservableProperty]
        private Server _editableServer = new();

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
                Servers.AddServer(EditableServer);
                Servers.RemoveServer(Server);

                notificationService.SendNotification($"{Server.Name} has been edited.");
                Server = new Server();
                _ = settings.SaveAsync();
                navigationService.Back();
            }
            catch (Exception ex)
            {
                notificationService.SendErrorNotification(ex.Message);
            }
        }

        partial void OnServerChanged(Server value)
        {
            EditableServer = (Server)value.Clone();
        }
    }
}