using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class ServersViewModel(NavigationService navigationService, NotificationService notificationService, SettingsService settings)
        : ViewModelBase
    {
        [ObservableProperty]
        private ServersSettings _servers = settings.Get<ServersSettings>();

        [ObservableProperty]
        private Server? _selectedServer;

        partial void OnSelectedServerChanged(Server? value)
        {
            if (value == null) return;
            var vm = navigationService.NavigateTo<SelectedServerViewModel>();
            vm.SelectedServer = value;
            SelectedServer = null;
        }

        [RelayCommand]
        private void DeleteServer(Server server)
        {
            Servers.RemoveServer(server);
            notificationService.SendSuccessNotification($"{server.Name} has been removed.");
            _ = settings.SaveAsync();
        }

        [RelayCommand]
        private void EditServer(Server? server)
        {
            if (server == null) return; //Somehow can be null.
            var vm = navigationService.NavigateTo<EditServerViewModel>();
            vm.Server = server;
        }
    }
}