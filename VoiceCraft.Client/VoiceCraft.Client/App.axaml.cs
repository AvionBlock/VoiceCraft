using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.Styling;
using Jeek.Avalonia.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Locales;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.Views;
using VoiceCraft.Client.Views.Error;

namespace VoiceCraft.Client;

public class App : Application
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();

    //Speex DSP
    private static readonly Guid SpeexDspEchoCancelerGuid = Guid.Parse("b4844eca-d5c0-497a-9819-7e4fa9ffa7ed");
    private static readonly Guid SpeexDspAutomaticGainControllerGuid = Guid.Parse("AE3F02FF-41A7-41FD-87A0-8EB0DA82B21C");
    private static readonly Guid SpeexDspDenoiserGuid = Guid.Parse("6E911874-5D10-4C8C-8E0A-6B30DF16EF78");

    //Background Images
    public static readonly Guid DockNightGuid = Guid.Parse("6b023e19-c9c5-4e06-84df-22833ccccd87");
    private static readonly Guid DockDayGuid = Guid.Parse("7c615c28-33b7-4d1d-b530-f8d988b00ea1");
    private static readonly Guid LethalCraftGuid = Guid.Parse("8d7616ce-cc2e-45af-a1c0-0456c09b998c");
    private static readonly Guid BlockSenseSpawnGuid = Guid.Parse("EDC317D4-687D-4607-ABE6-9C14C29054E9");
    private static readonly Guid SineSmpBaseGuid = Guid.Parse("3FAD5542-64F2-4A00-A4C2-534A517CCDE1");

    //Themes
    public static readonly Guid DarkThemeGuid = Guid.Parse("cf8e39fe-21cc-4210-91e6-d206e22ca52e");
    private static readonly Guid LightThemeGuid = Guid.Parse("3aeb95bc-a749-40f0-8f45-9f9070b76125");
    private static readonly Guid DarkPurpleThemeGuid = Guid.Parse("A59F5C67-043E-4052-A060-32D3DCBD43F7");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var serviceProvider = BuildServiceProvider();
            SetupServices(serviceProvider);

            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                    // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                    DisableAvaloniaDataAnnotationValidation();
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                    };

                    desktop.MainWindow.Closing += (__, ___) => { _ = serviceProvider.GetRequiredService<SettingsService>().SaveImmediate(); };
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                    // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                    DisableAvaloniaDataAnnotationValidation();
                    desktop.MainWindow = new ErrorMainWindow
                    {
                        DataContext = new ErrorViewModel { ErrorMessage = ex.ToString() }
                    };
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    singleViewPlatform.MainView = new ErrorView
                    {
                        DataContext = new ErrorViewModel { ErrorMessage = ex.ToString() }
                    };
                    break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        //Service Registry
        ServiceCollection.AddSingleton<NavigationService>(x =>
            new NavigationService(y => (ViewModelBase)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<INotificationMessageManager, NotificationMessageManager>();
        ServiceCollection.AddSingleton<NotificationService>();
        ServiceCollection.AddSingleton<PermissionsService>(x => new PermissionsService(x.GetRequiredService<NotificationService>(),
            y => (Permissions.BasePermission)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<ThemesService>();
        ServiceCollection.AddSingleton(SetupSettings());

        //Pages Registry
        ServiceCollection.AddSingleton<MainViewModel>();

        //Main Pages
        ServiceCollection.AddTransient<HomeViewModel>();
        ServiceCollection.AddSingleton<EditServerViewModel>();
        ServiceCollection.AddSingleton<AddServerViewModel>();

        //Home Pages
        ServiceCollection.AddTransient<ServersViewModel>();
        ServiceCollection.AddTransient<SettingsViewModel>();
        ServiceCollection.AddTransient<SelectedServerViewModel>();
        ServiceCollection.AddTransient<CreditsViewModel>();
        
        //Add Available Permissions
        ServiceCollection.AddTransient<Permissions.PostNotifications>();

        return ServiceCollection.BuildServiceProvider();
    }

    private static void SetupServices(IServiceProvider serviceProvider)
    {
        Localizer.SetLocalizer(new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales"));
        
        var audioService = serviceProvider.GetRequiredService<AudioService>();
        audioService.RegisterEchoCanceler<SpeexDspEchoCanceler>(SpeexDspEchoCancelerGuid, "SpeexDsp Echo Canceler");
        audioService.RegisterAutomaticGainController<SpeexDspAutomaticGainController>(SpeexDspAutomaticGainControllerGuid,
            "SpeexDsp Automatic Gain Controller");
        audioService.RegisterDenoiser<SpeexDspDenoiser>(SpeexDspDenoiserGuid, "SpeexDsp Denoiser");

        var themesService = serviceProvider.GetRequiredService<ThemesService>();
        themesService.RegisterTheme(DarkThemeGuid, "Dark", [new Themes.Dark.Styles()], [new Themes.Dark.VcColors(), new Themes.Dark.Resources()],
            ThemeVariant.Dark);
        themesService.RegisterTheme(LightThemeGuid, "Light", [new Themes.Light.Styles()], [new Themes.Light.Colors(), new Themes.Light.Resources()],
            ThemeVariant.Light);
        themesService.RegisterTheme(DarkPurpleThemeGuid, "Dark Purple", [new Themes.DarkPurple.Styles()],
            [new Themes.DarkPurple.Colors(), new Themes.DarkPurple.Resources()], ThemeVariant.Dark);
        themesService.RegisterBackgroundImage(DockNightGuid, "Dock Night", "avares://VoiceCraft.Client/Assets/bgdark.png");
        themesService.RegisterBackgroundImage(DockDayGuid, "Dock Day", "avares://VoiceCraft.Client/Assets/bglight.png");
        themesService.RegisterBackgroundImage(LethalCraftGuid, "Lethal Craft", "avares://VoiceCraft.Client/Assets/lethalCraft.png");
        themesService.RegisterBackgroundImage(BlockSenseSpawnGuid, "BlockSense Spawn", "avares://VoiceCraft.Client/Assets/blocksensespawn.jpg");
        themesService.RegisterBackgroundImage(SineSmpBaseGuid, "SineSMP Base", "avares://VoiceCraft.Client/Assets/sinesmpbase.png");
    }

    private static SettingsService SetupSettings()
    {
        var settings = new SettingsService();
        settings.RegisterSetting<AudioSettings>();
        settings.RegisterSetting<NotificationSettings>();
        settings.RegisterSetting<ServersSettings>();
        settings.RegisterSetting<ThemeSettings>();
        settings.RegisterSetting<LocaleSettings>();
        settings.Load();
        return settings;
    }
}