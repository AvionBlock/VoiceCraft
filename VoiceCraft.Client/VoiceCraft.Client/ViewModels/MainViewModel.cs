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
                    previousViewModel.OnDisappearing();
                Content = viewModel;
                viewModel.OnAppearing();
            };
            // change to HomeView 
            router.GoTo<HomeViewModel>();
        }
    }
}