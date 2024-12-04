using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _content = default!;
        public MainViewModel(NavigationService navigationService)
        {
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            navigationService.OnViewModelChanged += viewModel =>
            {
                if(Content is ViewModelBase previousViewModel)
                    previousViewModel.OnDisappearing();
                Content = viewModel;
                viewModel.OnAppearing();
            };
            // change to HomeView 
            navigationService.NavigateTo<HomeViewModel>();
        }
    }
}