using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class EditServerViewModel : ViewModelBase
    {
        public override string Title => "Edit Server";

        private NotificationSettings _notificationSettings;
        private NavigationService _navigator;
        private INotificationMessageManager _manager;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server _server = new Server();

        public EditServerViewModel(NavigationService navigator, NotificationMessageManager manager, SettingsService settings)
        {
            _navigator = navigator;
            _manager = manager;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);
        }

        [RelayCommand]
        public void Cancel()
        {
            _navigator.Back();
        }

        [RelayCommand]
        public void EditServer()
        {
            try
            {
                Servers.RemoveServer(Server);
                Servers.AddServer(Server);

                if (!_notificationSettings.DisableNotifications)
                {
                    _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundBrush"))
                    .HasBadge("Server")
                    .HasMessage($"{Server.Name} has been edited.")
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
                }
                Server = new Server();
                _ = _settings.SaveAsync();
                _navigator.Back();
            }
            catch (Exception ex)
            {
                if (!_notificationSettings.DisableNotifications)
                {
                    _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(ex.Message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
                }
            }
        }
    }
}