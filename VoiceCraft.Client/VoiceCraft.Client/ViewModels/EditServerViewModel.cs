using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Settings;

namespace VoiceCraft.Client.ViewModels
{
    public partial class EditServerViewModel : ViewModelBase
    {
        public override string Title { get; protected set; } = "Edit Server";

        private NotificationService _notificationService;
        private HistoryRouter<ViewModelBase> _router;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;
        [ObservableProperty]
        private Server _server = new Server();

        [ObservableProperty]
        private Server _editableServer = new Server();

        public EditServerViewModel(HistoryRouter<ViewModelBase> router, NotificationService notificationService, SettingsService settings)
        {
            _router = router;
            _settings = settings;
            _notificationService = notificationService;
            _servers = settings.Get<ServersSettings>();
        }

        [RelayCommand]
        public void Cancel()
        {
            _router.Back();
        }

        [RelayCommand]
        public void EditServer()
        {
            try
            {
                Servers.AddServer(EditableServer);
                Servers.RemoveServer(Server);

                _notificationService.SendNotification($"{Server.Name} has been edited.");
                Server = new Server();
                _ = _settings.SaveAsync();
                _router.Back();
            }
            catch (Exception ex)
            {
                _notificationService.SendErrorNotification(ex.Message);
            }
        }

        partial void OnServerChanged(Server value)
        {
            EditableServer = (Server)value.Clone();
        }
    }
}
