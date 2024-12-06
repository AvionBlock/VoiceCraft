using Avalonia.Media.Imaging;
using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private Bitmap? _backgroundImage;
        [ObservableProperty] private object _content = default!;

        [ObservableProperty] private INotificationMessageManager _manager;
        
        public MainViewModel(NavigationService navigationService, INotificationMessageManager manager, ThemesService themesService, SettingsService settingsService)
        {
            _manager = manager;
            themesService.OnBackgroundImageChanged += (backgroundImage) =>
            {
                BackgroundImage = backgroundImage?.BackgroundImageBitmap;
            };
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
            var themeSettings = settingsService.Get<ThemeSettings>();
            themesService.SwitchTheme(themeSettings.SelectedTheme);
            themesService.SwitchBackgroundImage(themeSettings.SelectedBackgroundImage);
            navigationService.NavigateTo<HomeViewModel>();
        }
    }
}