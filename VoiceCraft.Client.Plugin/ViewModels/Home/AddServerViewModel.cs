using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class AddServerViewModel : ViewModelBase
    {
        public override string Title => "Add Server";

        private NotificationSettings _notificationSettings;
        private INotificationMessageManager _manager;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server _server = new();

        public AddServerViewModel(NotificationMessageManager manager, SettingsService settings)
        {
            _manager = manager;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(Plugin.PluginId);
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);
        }

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Servers.AddServer(Server);
                if (!_notificationSettings.DisableNotifications)
                {
                    _manager.CreateMessage()
                        .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                        .Animates(true)
                        .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                        .HasBadge("Server")
                        .HasMessage($"{Server.Name} has been added.")
                        .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                        .Dismiss().WithButton("Dismiss", (button) => { })
                        .Queue();
                }

                Server = new Server();
                _ = _settings.SaveAsync();
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