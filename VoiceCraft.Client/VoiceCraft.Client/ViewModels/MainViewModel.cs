using Avalonia.Media.Imaging;
using Avalonia.Notification;
using CommunityToolkit.Mvvm.ComponentModel;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private Bitmap? _backgroundImage;
    [ObservableProperty] private object? _content;

    [ObservableProperty] private INotificationMessageManager _manager;

    public MainViewModel(NavigationService navigationService, INotificationMessageManager manager, ThemesService themesService,
        SettingsService settingsService, BackgroundService backgroundService, DiscordRpcService discordRpcService)
    {
        _manager = manager;
        themesService.OnBackgroundImageChanged += backgroundImage => { BackgroundImage = backgroundImage?.BackgroundImageBitmap; };
        // register route changed event to set content to viewModel, whenever 
        // a route changes
        navigationService.OnViewModelChanged += viewModel =>
        {
            Content = viewModel;
            discordRpcService.SetState($"In page {viewModel.GetType().Name.Replace("ViewModel", "")}");
        };
        var themeSettings = settingsService.ThemeSettings;
        themesService.SwitchTheme(themeSettings.SelectedTheme);
        themesService.SwitchBackgroundImage(themeSettings.SelectedBackgroundImage);
        var localeSettings = settingsService.LocaleSettings;
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
        navigationService.NavigateTo<VoiceViewModel>(new VoiceNavigationData(process));
    }
}