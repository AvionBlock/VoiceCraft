using Avalonia;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title { get => "Servers"; protected set => throw new NotSupportedException(); }
        private HistoryRouter<ViewModelBase> _router;
        private INotificationMessageManager _manager;

        [ObservableProperty]
        private ServerModel? _selectedServer;

        [ObservableProperty]
        private SettingsModel _settings = default!;

        partial void OnSelectedServerChanged(ServerModel? value)
        {
            if (value == null) return;
            var model = _router.GoTo<ServerViewModel>();
            model.SelectedServer = value;
            SelectedServer = null;
        }

        public ServersViewModel(HistoryRouter<ViewModelBase> router, NotificationMessageManager manager, SettingsModel settings)
        {
            _manager = manager;
            _router = router;
            _settings = settings;
        }

        [RelayCommand]
        public async Task DeleteServer(ServerModel server)
        {
            Settings.RemoveServer(server);
            _manager.CreateMessage()
                .Accent("#1751C3")
                .Animates(true)
                .Background("#333")
                .HasBadge("Info")
                .HasMessage($"{server.Name} has been removed.")
                .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                .Queue();
            await Settings.SaveAsync();
        }

        [RelayCommand]
        public void EditServer(ServerModel? server)
        {
            if (server == null) return; //Somehow can be null.
            var model = _router.GoTo<EditServerViewModel>();
            model.Server = server;
        }
    }
}
