using Avalonia.Media.Imaging;
using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private Bitmap? _backgroundImage;
        [ObservableProperty] private object? _content;

        [ObservableProperty] private INotificationMessageManager _manager;

        public MainViewModel(NavigationService navigationService, INotificationMessageManager manager, ThemesService themesService,
            SettingsService settingsService, BackgroundService backgroundService)
        {
            _manager = manager;
            themesService.OnBackgroundImageChanged += (backgroundImage) => { BackgroundImage = backgroundImage?.BackgroundImageBitmap; };
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            navigationService.OnViewModelChanged += viewModel =>
            {
                if (Content is ViewModelBase previousViewModel)
                    previousViewModel.OnDisappearing();
                Content = viewModel;
                viewModel.OnAppearing();
            };
            var themeSettings = settingsService.Get<ThemeSettings>();
            themesService.SwitchTheme(themeSettings.SelectedTheme);
            themesService.SwitchBackgroundImage(themeSettings.SelectedBackgroundImage);
            var localeSettings = settingsService.Get<LocaleSettings>();
            try
            {
                Localizer.Language = localeSettings.Culture;
            }
            catch
            {
                Localizer.LanguageIndex = 0;
            }

            // change to HomeView 
            navigationService.NavigateTo<HomeViewModel>();

            backgroundService.TryGetBackgroundProcess<VoipBackgroundProcess>(out var process);
            if (process == null) return;
            navigationService.NavigateTo<VoiceViewModel>().AttachToProcess(process);
        }
    }
}