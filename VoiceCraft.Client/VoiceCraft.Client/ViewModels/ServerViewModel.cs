using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class ServerViewModel : ViewModelBase
    {
        private readonly HistoryRouter<ViewModelBase> _router;

        [ObservableProperty]
        private Server _selectedServer = new();

        [ObservableProperty]
        private string _pingTime = "127ms";

        [ObservableProperty]
        private string _motd = "WELCOME TO THE COOL SERVER!";

        [ObservableProperty]
        private string _connectedParticipants = "2";

        public ServerViewModel(HistoryRouter<ViewModelBase> router)
        {
            _router = router;
        }

        [RelayCommand]
        public void Cancel()
        {
            _router.Back();
        }

        [RelayCommand]
        public void Connect()
        {
        }
    }
}