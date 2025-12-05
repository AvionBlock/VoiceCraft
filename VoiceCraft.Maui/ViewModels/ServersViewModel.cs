using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServersViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        ObservableCollection<ServerModel> servers = [];

        [ObservableProperty]
        SettingsModel settings;

        public ServersViewModel(IDatabaseService databaseService, INavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;

            LoadServers();
            Settings = _databaseService.Settings;

            _databaseService.OnServerAdded += ServerAdded;
            _databaseService.OnServerRemoved += ServerRemoved;
        }

        private async void LoadServers()
        {
            await _databaseService.Initialization;
            foreach (var server in _databaseService.Servers)
            {
                Servers.Add(server);
            }
        }

        [RelayCommand]
        public async Task DeleteServer(ServerModel server)
        {
            await _databaseService.RemoveServer(server);
        }

        [RelayCommand]
        public async Task GoToAddServer()
        {
            await _navigationService.NavigateTo(nameof(AddServer));
        }

        [RelayCommand]
        public async Task GoToEditServer(ServerModel server)
        {
            await _navigationService.NavigateTo(nameof(EditServer), server);
        }

        [RelayCommand]
        public async Task GoToServer(ServerModel server)
        {
            await _navigationService.NavigateTo(nameof(ServerDetails), server);
        }

        private void ServerAdded(ServerModel server)
        {
            Servers.Add(server);
        }

        private void ServerRemoved(ServerModel server)
        {
            Servers.Remove(server);
        }
    }
}
