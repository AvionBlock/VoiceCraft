using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Settings;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class AddServerViewModel : ViewModelBase
    {
        public override string Title { get; protected set; } = "Add Server";

        private NotificationService _notificationService;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server _server = new();

        public AddServerViewModel(NotificationService notificationService, SettingsService settings)
        {
            _notificationService = notificationService;
            _settings = settings;
            _servers = settings.Get<ServersSettings>();
        }

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Servers.AddServer(Server);

                _notificationService.SendSuccessNotification($"{Server.Name} has been added.");
                Server = new Server();
                _ = _settings.SaveAsync();
            }
            catch (Exception ex)
            {
                _notificationService.SendErrorNotification(ex.Message);
            }
        }
    }
}
