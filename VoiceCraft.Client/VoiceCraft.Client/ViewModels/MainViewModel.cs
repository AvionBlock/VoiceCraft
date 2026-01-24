using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private Bitmap? _backgroundImage;
    [ObservableProperty] private object? _content;

    public MainViewModel(NavigationService navigationService,
        ThemesService themesService,
        SettingsService settingsService,
        DiscordRpcService discordRpcService,
        HotKeyService hotKeyService,
        IBackgroundService backgroundService)
    {
        themesService.OnBackgroundImageChanged += backgroundImage =>
        {
            BackgroundImage = backgroundImage?.BackgroundImageBitmap;
        };
        // register route changed event to set content to viewModel, whenever 
        // a route changes
        navigationService.OnViewModelChanged += viewModel =>
        {
            Content = viewModel;
            discordRpcService.SetState($"In page {viewModel.GetType().Name.Replace("ViewModel", "")}");
        };
        //Initialize Themes
        var themeSettings = settingsService.ThemeSettings;
        themesService.SwitchTheme(themeSettings.SelectedTheme);
        themesService.SwitchBackgroundImage(themeSettings.SelectedBackgroundImage);
        //Initialize Locale Settings
        var localeSettings = settingsService.LocaleSettings;
        Localizer.Instance.Language = localeSettings.Culture;
        hotKeyService.Initialize(); //Initialize the hotkey service.

        // change to HomeView 
        navigationService.NavigateTo<HomeViewModel>();
        
        var voiceCraftService = backgroundService.GetService<VoiceCraftService>();
        if (voiceCraftService == null) return;
        navigationService.NavigateTo<VoiceViewModel>(new VoiceNavigationData(voiceCraftService));
    }
}