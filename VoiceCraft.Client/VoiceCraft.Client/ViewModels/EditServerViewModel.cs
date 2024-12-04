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
        [ObservableProperty] private Server _server = new();

        [ObservableProperty] private Server _editableServer = new();

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
                var serversSettings = settings.Get<ServersSettings>();
                serversSettings.AddServer(EditableServer);
                serversSettings.RemoveServer(Server);

                notificationService.SendNotification($"{Server.Name} has been edited.");
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
}