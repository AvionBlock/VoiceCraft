using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class ServersViewModel(NavigationService navigationService, NotificationService notificationService, SettingsService settings)
        : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        private ServersSettingsViewModel _serversSettings = new(settings.Get<ServersSettings>(), settings);

        [ObservableProperty]
        private ServerViewModel? _selectedServer;

        partial void OnSelectedServerChanged(ServerViewModel? value)
        {
            if (value == null) return;
            var vm = navigationService.NavigateTo<SelectedServerViewModel>();
            vm.SelectedServer = value;
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
            notificationService.SendSuccessNotification($"{server.Name} has been removed.");
            _ = settings.SaveAsync();
        }

        [RelayCommand]
        private void EditServer(ServerViewModel? server)
        {
            if (server == null) return;
            var vm = navigationService.NavigateTo<EditServerViewModel>();
            vm.Server = server.Server;
            vm.EditableServer = (Server)server.Server.Clone();
        }

        public void Dispose()
        {
            ServersSettings.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}