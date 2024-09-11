using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class AddServerViewModel : ViewModelBase
    {
        public override string Title { get => "Add Server"; protected set => throw new NotSupportedException(); }
        private INotificationMessageManager _manager;

        [ObservableProperty]
        private SettingsModel _settings;

        [ObservableProperty]
        private ServerModel _server = new ServerModel("", "", 9050, 0);

        public AddServerViewModel(SettingsModel settings, NotificationMessageManager manager)
        {
            _manager = manager;
            _settings = settings;
        }

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Settings.AddServer(Server);
                _manager.CreateMessage()
                    .Accent("#1751C3")
                    .Animates(true)
                    .Background("#333")
                    .HasBadge("Info")
                    .HasMessage($"{Server.Name} has been added.")
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(3))
                    .Queue();
                Server = new ServerModel("", "", 9050, 0);
                _ = Settings.SaveAsync();
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
