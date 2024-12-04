using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SelectedServerViewModel(NavigationService navigationService) : ViewModelBase
    {
        [ObservableProperty]
        private Server _selectedServer = new();

        [ObservableProperty]
        private string _pingTime = "127ms";

        [ObservableProperty]
        private string _motd = "WELCOME TO THE COOL SERVER!";

        [ObservableProperty]
        private string _connectedParticipants = "2";

        [RelayCommand]
        private void Cancel()
        {
            navigationService.Back();
        }

        [RelayCommand]
        private void Connect()
        {
        }
    }
}