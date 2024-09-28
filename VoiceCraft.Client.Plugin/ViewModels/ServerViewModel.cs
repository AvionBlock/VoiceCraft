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
        private string _pingInfo = "Pinging...";

        public ServerViewModel(NavigationService navigator)
        {
            _navigator = navigator;
        }

        [RelayCommand]
        public void Cancel()
        {
            PingInfo = "Pinging...";
            _navigator.Back();
        }

        [RelayCommand]
        public void Connect()
        {
        }
    }
}