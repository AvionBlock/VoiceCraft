using Avalonia.Controls;
using Avalonia.Notification;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Views;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IMainViewModel
    {
        [ObservableProperty]
        private Control? _content = default!;

        [ObservableProperty]
        private INotificationMessageManager _manager;

        public MainViewModel(NotificationMessageManager manager, HomeView homeView)
        {
            Manager = manager;
            // register route changed event to set content to viewModel, whenever
            // a route changes
            _content = homeView;

            Task.Delay(5000).ContinueWith(x =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    Manager
                .CreateMessage()
                .Accent("#1751C3")
                .Animates(true)
                .Background("#333")
                .HasBadge("Info")
                .HasMessage(
                    "Update will be installed on next application restart. This message will be dismissed after 5 seconds.")
                .Dismiss().WithButton("Update now", button => { })
                .Dismiss().WithButton("Release notes", button => { })
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                .Queue();
                });
            });
        }
    }
}
