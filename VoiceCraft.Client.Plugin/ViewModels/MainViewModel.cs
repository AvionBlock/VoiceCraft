using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IMainViewModel
    {
        public override string Title => "Main";

        [ObservableProperty]
        private ViewBase? _content = default!;

        [ObservableProperty]
        private INotificationMessageManager _manager;

        public MainViewModel(NotificationMessageManager manager, NavigationService navigation)
        {
            Manager = manager;
            // register route changed event to set content to viewModel, whenever
            // a route changes
            navigation.OnPageChanged += (s, p) =>
            {
                Content = p;
            };
        }
    }
}
