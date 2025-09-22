using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Locales;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.ViewModels.Settings;
using VoiceCraft.Client.Views;
using VoiceCraft.Client.Views.Error;
using VoiceCraft.Client.Views.Home;
using VoiceCraft.Client.Views.Settings;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var serviceProvider = BuildServiceProvider();
            SetupServices(serviceProvider);

            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
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
                    desktop.MainWindow = new ErrorMainWindow
                    {
                        Content = new ErrorView(),
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
            LogService.Log(ex);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
#pragma warning disable IL2026
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
#pragma warning restore IL2026
    }

    private static ServiceProvider BuildServiceProvider()
    {
        //Service Registry
        ServiceCollection.AddSingleton<DiscordRpcService>();
        ServiceCollection.AddSingleton<ViewLocatorService>(x => new ViewLocatorService(x.GetKeyedService<Control>));
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
        ServiceCollection.AddTransient<GeneralSettingsViewModel>();
        ServiceCollection.AddTransient<AppearanceSettingsViewModel>();
        ServiceCollection.AddTransient<AudioSettingsViewModel>();
        ServiceCollection.AddTransient<NetworkSettingsViewModel>();
        ServiceCollection.AddTransient<HotKeySettingsViewModel>();
        ServiceCollection.AddTransient<AdvancedSettingsViewModel>();

        //Home Pages
        ServiceCollection.AddSingleton<AddServerViewModel>();
        ServiceCollection.AddSingleton<ServersViewModel>();
        ServiceCollection.AddSingleton<SettingsViewModel>();
        ServiceCollection.AddSingleton<CreditsViewModel>();
        ServiceCollection.AddSingleton<CrashLogViewModel>();

        //Views
        ServiceCollection.AddKeyedTransient<Control, MainView>(typeof(MainView).FullName);
        ServiceCollection.AddKeyedTransient<Control, HomeView>(typeof(HomeView).FullName);
        ServiceCollection.AddKeyedTransient<Control, EditServerView>(typeof(EditServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, SelectedServerView>(typeof(SelectedServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, VoiceView>(typeof(VoiceView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AddServerView>(typeof(AddServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, ServersView>(typeof(ServersView).FullName);
        ServiceCollection.AddKeyedTransient<Control, SettingsView>(typeof(SettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, CreditsView>(typeof(CreditsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, CrashLogView>(typeof(CrashLogView).FullName);
        ServiceCollection.AddKeyedTransient<Control, GeneralSettingsView>(typeof(GeneralSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AppearanceSettingsView>(typeof(AppearanceSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AudioSettingsView>(typeof(AudioSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, NetworkSettingsView>(typeof(NetworkSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, HotKeySettingsView>(typeof(HotKeySettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AdvancedSettingsView>(typeof(AdvancedSettingsView).FullName);

        //Themes Registry
        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkThemeGuid,
            "Dark",
            ThemeVariant.Dark,
            [new Themes.Dark.Styles()],
            [new Themes.Dark.Colors(), new Themes.Dark.Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkPurpleThemeGuid,
            "Dark Purple",
            ThemeVariant.Dark,
            [new Themes.DarkPurple.Styles()],
            [new Themes.DarkPurple.Colors(), new Themes.DarkPurple.Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkGreenThemeGuid,
            "Dark Green",
            ThemeVariant.Dark,
            [new Themes.DarkGreen.Styles()],
            [new Themes.DarkGreen.Colors(), new Themes.DarkGreen.Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.LightThemeGuid,
            "Light",
            ThemeVariant.Light,
            [new Themes.Light.Styles()],
            [new Themes.Light.Colors(), new Themes.Light.Resources()]));

        //Background Image Registry
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.DockNightGuid,
            "Dock Night",
            "avares://VoiceCraft.Client/Assets/bgdark.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.DockDayGuid,
            "Dock Day",
            "avares://VoiceCraft.Client/Assets/bglight.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.LethalCraftGuid,
            "Lethal Craft",
            "avares://VoiceCraft.Client/Assets/lethalCraft.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.BlockSenseSpawnGuid,
            "BlockSense Spawn",
            "avares://VoiceCraft.Client/Assets/blocksensespawn.jpg"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.SineSmpBaseGuid,
            "SineSMP Base",
            "avares://VoiceCraft.Client/Assets/sinesmpbase.png"));
        
        //HotKey Registry
        ServiceCollection.AddSingleton<HotKeyAction, MuteAction>();
        ServiceCollection.AddSingleton<HotKeyAction, DeafenAction>();

        return ServiceCollection.BuildServiceProvider();
    }

    private void SetupServices(IServiceProvider serviceProvider)
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales");
        DataTemplates.Add(serviceProvider.GetRequiredService<ViewLocatorService>());
        serviceProvider.GetService<HotKeyService>();
    }
}