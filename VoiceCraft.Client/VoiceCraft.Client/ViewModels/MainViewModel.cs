using Avalonia.Notification;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public override string Title { get => "Main View Model"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ViewModelBase? _content = default!;

        [ObservableProperty]
        private INotificationMessageManager _manager;

        public MainViewModel(HistoryRouter<ViewModelBase> router, NotificationMessageManager manager, ThemesService themes)
        {
            _manager = manager;
            // register route changed event to set content to viewModel, whenever
            // a route changes
            router.CurrentViewModelChanged += viewModel => Content = viewModel;
            themes.OnThemeChanged += (from, to) =>
            {
                //Stupid, but it works.
                var currContent = Content;
                Content = new RefreshingViewModel();
                Task.Delay(500).ContinueWith(x => Content = currContent);
            };

            router.GoTo<HomeViewModel>();
        }
    }
}
