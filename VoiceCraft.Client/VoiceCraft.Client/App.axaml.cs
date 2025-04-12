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
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Locales;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.Views;
using VoiceCraft.Client.Views.Error;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Client;

public class App : Application
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    public static IServiceProvider? ServiceProvider { get; private set; }

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
            
            ServiceProvider = serviceProvider;
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
        ServiceCollection.AddSingleton<SettingsService>();

        //Pages Registry
        ServiceCollection.AddSingleton<MainViewModel>();

        //Main Pages
        ServiceCollection.AddSingleton<HomeViewModel>();
        ServiceCollection.AddTransient<EditServerViewModel>();
        ServiceCollection.AddTransient<SelectedServerViewModel>();
        ServiceCollection.AddTransient<VoiceViewModel>();

        //Home Pages
        ServiceCollection.AddSingleton<AddServerViewModel>();
        ServiceCollection.AddSingleton<ServersViewModel>();
        ServiceCollection.AddSingleton<SettingsViewModel>();
        ServiceCollection.AddSingleton<CreditsViewModel>();
        ServiceCollection.AddSingleton<CrashLogViewModel>();

        //Add Available Permissions
        ServiceCollection.AddTransient<Permissions.PostNotifications>();

        return ServiceCollection.BuildServiceProvider();
    }

    private static void SetupServices(IServiceProvider serviceProvider)
    {
        Localizer.SetLocalizer(new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales"));

        var themesService = serviceProvider.GetRequiredService<ThemesService>();
        themesService.RegisterTheme(Constants.DarkThemeGuid, "Dark",
            [
                new Themes.Dark.Styles()
            ],
            [
                new Themes.Dark.VcColors(),
                new Themes.Dark.Resources()
            ],
            ThemeVariant.Dark);
        
        themesService.RegisterTheme(Constants.LightThemeGuid, "Light",
            [
                new Themes.Light.Styles()
            ],
            [
                new Themes.Light.Colors(),
                new Themes.Light.Resources()
            ],
            ThemeVariant.Light);
        
        themesService.RegisterTheme(Constants.DarkPurpleThemeGuid, "Dark Purple",
            [
                new Themes.DarkPurple.Styles()
            ],
            [
                new Themes.DarkPurple.Colors(),
                new Themes.DarkPurple.Resources()
            ],
            ThemeVariant.Dark);
        
        themesService.RegisterTheme(Constants.DarkGreenThemeGuid, "Dark Green",
            [
                new Themes.DarkGreen.Styles()
            ],
            [
                new Themes.DarkGreen.Colors(),
                new Themes.DarkGreen.Resources()
            ],
            ThemeVariant.Dark);
        
        themesService.RegisterBackgroundImage(Constants.DockNightGuid, "Dock Night", "avares://VoiceCraft.Client/Assets/bgdark.png");
        themesService.RegisterBackgroundImage(Constants.DockDayGuid, "Dock Day", "avares://VoiceCraft.Client/Assets/bglight.png");
        themesService.RegisterBackgroundImage(Constants.LethalCraftGuid, "Lethal Craft", "avares://VoiceCraft.Client/Assets/lethalCraft.png");
        themesService.RegisterBackgroundImage(Constants.BlockSenseSpawnGuid, "BlockSense Spawn", "avares://VoiceCraft.Client/Assets/blocksensespawn.jpg");
        themesService.RegisterBackgroundImage(Constants.SineSmpBaseGuid, "SineSMP Base", "avares://VoiceCraft.Client/Assets/sinesmpbase.png");
    }
}