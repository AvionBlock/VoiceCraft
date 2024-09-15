using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public override string Title { get => "Main View Model"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ViewModelBase _content = default!;

        [ObservableProperty]
        private INotificationMessageManager _manager;

        public MainViewModel(NavigationService<ViewModelBase> navigationService, NotificationMessageManager manager)
        {
            _manager = manager;
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            navigationService.GoTo<HomeViewModel>();
        }
    }
}
