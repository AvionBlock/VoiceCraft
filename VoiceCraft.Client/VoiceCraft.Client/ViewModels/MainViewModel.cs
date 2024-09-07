using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public override string Title { get => "Main View Model"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ViewModelBase _content = default!;

        public MainViewModel(HistoryRouter<ViewModelBase> router)
        {
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            router.CurrentViewModelChanged += viewModel => Content = viewModel;

            // change to HomeView 
            router.GoTo<HomeViewModel>();
        }
    }
}
