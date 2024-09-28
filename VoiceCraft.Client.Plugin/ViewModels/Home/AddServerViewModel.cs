using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.Views.Home;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class AddServerViewModel : ViewModelBase
    {
        public override string Title => "Add Server";

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
        }

        [RelayCommand]
        public async Task AddServer()
        {
            try
            {
                Servers.AddServer(Server);
                _manager.CreateMessage()
                    .Accent("#1751C3")
                    .Animates(true)
                    .Background("#333")
                    .HasBadge("Info")
                    .HasMessage($"{Server.Name} has been added.")
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                    .Queue();
                Server = new Server();
                await _settings.SaveAsync();
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