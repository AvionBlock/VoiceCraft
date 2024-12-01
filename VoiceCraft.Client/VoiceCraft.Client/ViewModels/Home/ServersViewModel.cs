using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title { get; protected set; } = "Servers";

        private NotificationService _notificationService;
        HistoryRouter<ViewModelBase> _router;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server? _selectedServer;

        partial void OnSelectedServerChanged(Server? value)
        {
            if (value == null) return;
            var vm = _router.GoTo<ServerViewModel>();
            vm.SelectedServer = value;
            SelectedServer = null;
        }

        public ServersViewModel(HistoryRouter<ViewModelBase> router, NotificationService notificationService, SettingsService settings)
        {
            _router = router;
            _notificationService = notificationService;
            _settings = settings;
            _servers = settings.Get<ServersSettings>();
        }

        [RelayCommand]
        public void DeleteServer(Server server)
        {
            Servers.RemoveServer(server);
            _notificationService.SendSuccessNotification($"{server.Name} has been removed.");
            _ = _settings.SaveAsync();
        }

        [RelayCommand]
        public void EditServer(Server? server)
        {
            if (server == null) return; //Somehow can be null.
            var vm = _router.GoTo<EditServerViewModel>();
            vm.Server = server;
        }
    }
}
