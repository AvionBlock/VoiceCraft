using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.ViewModels;
using Avalonia.Notification;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class EditServerViewModel : ViewModelBase
    {
        public override string Title => "Edit Server";

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
        }

        [RelayCommand]
        public void Cancel()
        {
            _navigator.Back();
        }

        [RelayCommand]
        public async Task EditServer()
        {
            try
            {
                Servers.RemoveServer(Server);
                Servers.AddServer(Server);

                _manager.CreateMessage()
                .Accent("#1751C3")
                    .Animates(true)
                    .Background("#333")
                    .HasBadge("Info")
                    .HasMessage($"{Server.Name} has been edited.")
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                    .Queue();
                Server = new Server();
                await _settings.SaveAsync();
                _navigator.Back();
            }
            catch (Exception ex)
            {
                _manager.CreateMessage()
                    .Accent("#E0A030")
                    .Animates(true)
                    .Background("#333")
                    .HasBadge("Error")
                    .HasMessage(ex.Message)
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                    .Queue();
            }
        }
    }
}