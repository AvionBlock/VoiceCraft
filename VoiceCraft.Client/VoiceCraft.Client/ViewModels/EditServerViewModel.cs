using Avalonia.Notification;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels
{
    public partial class EditServerViewModel : ViewModelBase
    {
        public override string Title { get => "Edit Server"; protected set => throw new NotSupportedException(); }
        private HistoryRouter<ViewModelBase> _router;
        private INotificationMessageManager _manager;

        [ObservableProperty]
        private SettingsModel _settings;

        [ObservableProperty]
        private ServerModel _server = new ServerModel("", "", 9050, 0);

        public EditServerViewModel(HistoryRouter<ViewModelBase> router, NotificationMessageManager manager, SettingsModel settings)
        {
            _router = router;
            _manager = manager;
            _settings = settings;
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
                Settings.RemoveServer(Server);
                Settings.AddServer(Server);

                _manager.CreateMessage()
                .Accent("#1751C3")
                    .Animates(true)
                    .Background("#333")
                    .HasBadge("Info")
                    .HasMessage($"{Server.Name} has been edited.")
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                    .Queue();
                Server = new ServerModel("", "", 9050, 0);
                _ = Settings.SaveAsync();
                _router.Back();
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
