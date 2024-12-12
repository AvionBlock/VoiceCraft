using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SelectedServerViewModel(NavigationService navigationService, SettingsService settings) : ViewModelBase, IDisposable
    {
        [ObservableProperty]
        private ServersSettingsViewModel _serversSettings = new(settings.Get<ServersSettings>(), settings);
        
        [ObservableProperty]
        private ServerViewModel _selectedServer = new(new Server(), settings);

        [ObservableProperty]
        private string _pingTime = "127ms";

        [ObservableProperty]
        private string _motd = "WELCOME TO THE COOL SERVER!";

        [ObservableProperty]
        private string _connectedParticipants = "2";

        public void SetServer(Server server)
        {
            SelectedServer.Dispose();
            SelectedServer = new ServerViewModel(server, settings);
        }

        [RelayCommand]
        private void Cancel()
        {
            navigationService.Back();
        }

        [RelayCommand]
        private void Connect()
        {
        }
        
        public void Dispose()
        {
            SelectedServer.Dispose();
            ServersSettings.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}