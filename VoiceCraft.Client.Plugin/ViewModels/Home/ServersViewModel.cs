using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title => "Servers";

        private NotificationSettings _notificationSettings;
        private NavigationService _navigator;
        private INotificationMessageManager _manager;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server? _selectedServer;

        partial void OnSelectedServerChanged(Server? value)
        {
            if (value == null) return;
            var vm = _navigator.NavigateTo<ServerViewModel>();
            vm.SelectedServer = value;
            SelectedServer = null;
        }

        public ServersViewModel(NavigationService navigator, NotificationMessageManager manager, SettingsService settings)
        {
            _navigator = navigator;
            _manager = manager;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);
        }

        [RelayCommand]
        public void DeleteServer(Server server)
        {
            Servers.RemoveServer(server);
            if (!_notificationSettings.DisableNotifications)
            {
                _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                    .HasBadge("Server")
                    .HasMessage($"{server.Name} has been removed.")
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
            _ = _settings.SaveAsync();
        }

        [RelayCommand]
        public void EditServer(Server? server)
        {
            if (server == null) return; //Somehow can be null.
            var vm = _navigator.NavigateTo<EditServerViewModel>();
            vm.Server = server;
        }
    }
}