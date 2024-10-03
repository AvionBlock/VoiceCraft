using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.ViewModels;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class ServerViewModel : ViewModelBase
    {
        public override string Title => "Server";

        private NavigationService _navigator;

        [ObservableProperty]
        private Server _selectedServer = new();

        [ObservableProperty]
        private string _pingTime = "127ms";

        [ObservableProperty]
        private string _MOTD = "WELCOME TO THE COOL SERVER!";

        [ObservableProperty]
        private string _connectedParticipants = "2";

        public ServerViewModel(NavigationService navigator)
        {
            _navigator = navigator;
        }

        [RelayCommand]
        public void Cancel()
        {
            _navigator.Back();
        }

        [RelayCommand]
        public void Connect()
        {
        }
    }
}