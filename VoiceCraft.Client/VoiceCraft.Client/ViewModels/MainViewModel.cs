using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _content = default!;

        public MainViewModel(HistoryRouter<ViewModelBase> router)
        {
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            router.CurrentViewModelChanged += viewModel =>
            {
                if(Content is ViewModelBase previousViewModel)
                    previousViewModel.OnDisappearing(this);

                Content = viewModel;

                if (viewModel is ViewModelBase newViewModel)
                    newViewModel.OnAppearing(this);
            };

            // change to HomeView 
            router.GoTo<HomeViewModel>();
        }
    }
}