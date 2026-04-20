using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using SoundFlow.Abstracts;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Locales;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Themes.Dark;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.ViewModels.Settings;
using VoiceCraft.Client.Views;
using VoiceCraft.Client.Views.Error;
using VoiceCraft.Client.Views.Home;
using VoiceCraft.Client.Views.Settings;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Telemetry;
using Styles = VoiceCraft.Client.Themes.Dark.Styles;

namespace VoiceCraft.Client;

public class App : Application
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    public static ServiceProvider? ServiceProvider { get; private set; }

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
            ConfigureTelemetry(serviceProvider);

            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                    };
                    serviceProvider.GetRequiredService<ClipboardService>().RegisterTopLevel(desktop.MainWindow);

                    desktop.MainWindow.Closing += (__, ___) =>
                    {
                        _ = serviceProvider.GetRequiredService<SettingsService>().SaveImmediate();
                    };
                    break;
                case IActivityApplicationLifetime activityLifetime:
                    activityLifetime.MainViewFactory = () =>
                    {
                        var mainView = new MainView
                        {
                            DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                        };
                        RegisterClipboardWhenAttached(serviceProvider, mainView);
                        return mainView;
                    };
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    var singleView = new MainView
                    {
                        DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                    };
                    RegisterClipboardWhenAttached(serviceProvider, singleView);
                    singleViewPlatform.MainView = singleView;
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
                case IActivityApplicationLifetime activityLifetime:
                    activityLifetime.MainViewFactory = () => new MainView()
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

            LogService.Log(ex);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        //Service Registry
        ServiceCollection.AddSingleton<DiscordRpcService>();
        ServiceCollection.AddSingleton<ViewLocatorService>(x => new ViewLocatorService(x.GetKeyedService<Control>));
        ServiceCollection.AddSingleton<NavigationService>(x =>
            new NavigationService(y => (ViewModelBase)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<NotificationService>();
        ServiceCollection.AddSingleton<ClipboardService>();
        ServiceCollection.AddSingleton<PermissionsService>(x => new PermissionsService(
            x.GetRequiredService<NotificationService>(),
            y => (Permissions.BasePermission)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<ThemesService>();
        ServiceCollection.AddSingleton<SettingsService>();
        ServiceCollection.AddSingleton<VoiceCraftService>();

        //Pages Registry
        ServiceCollection.AddSingleton<MainViewModel>();
        ServiceCollection.AddTransient<TelemetryConsentViewModel>();

        //Main Pages
        ServiceCollection.AddSingleton<HomeViewModel>();
        ServiceCollection.AddTransient<EditServerViewModel>();
        ServiceCollection.AddTransient<GeneralSettingsViewModel>();
        ServiceCollection.AddTransient<AppearanceSettingsViewModel>();
        ServiceCollection.AddTransient<InputSettingsViewModel>();
        ServiceCollection.AddTransient<OutputSettingsViewModel>();
        ServiceCollection.AddTransient<NetworkSettingsViewModel>();
        ServiceCollection.AddTransient<HotKeySettingsViewModel>();
        ServiceCollection.AddTransient<AdvancedSettingsViewModel>();
        ServiceCollection.AddTransient<SelectedServerViewModel>();
        ServiceCollection.AddTransient<VoiceViewModel>();

        //Home Pages
        ServiceCollection.AddSingleton<AddServerViewModel>();
        ServiceCollection.AddSingleton<ServersViewModel>();
        ServiceCollection.AddSingleton<SettingsViewModel>();
        ServiceCollection.AddSingleton<CreditsViewModel>();
        ServiceCollection.AddSingleton<CrashLogViewModel>();

        //Views
        ServiceCollection.AddKeyedTransient<Control, MainView>(typeof(MainView).FullName);
        ServiceCollection.AddKeyedTransient<Control, TelemetryConsentView>(typeof(TelemetryConsentView).FullName);
        ServiceCollection.AddKeyedTransient<Control, HomeView>(typeof(HomeView).FullName);
        ServiceCollection.AddKeyedTransient<Control, EditServerView>(typeof(EditServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AddServerView>(typeof(AddServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, ServersView>(typeof(ServersView).FullName);
        ServiceCollection.AddKeyedTransient<Control, SelectedServerView>(typeof(SelectedServerView).FullName);
        ServiceCollection.AddKeyedTransient<Control, SettingsView>(typeof(SettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, CreditsView>(typeof(CreditsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, CrashLogView>(typeof(CrashLogView).FullName);
        ServiceCollection.AddKeyedTransient<Control, GeneralSettingsView>(typeof(GeneralSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AppearanceSettingsView>(typeof(AppearanceSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, InputSettingsView>(typeof(InputSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, OutputSettingsView>(typeof(OutputSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, NetworkSettingsView>(typeof(NetworkSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, HotKeySettingsView>(typeof(HotKeySettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, AdvancedSettingsView>(typeof(AdvancedSettingsView).FullName);
        ServiceCollection.AddKeyedTransient<Control, VoiceView>(typeof(VoiceView).FullName);

        //Themes Registry
        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkThemeGuid,
            "ThemesService.Themes.Dark",
            ThemeVariant.Dark,
            [new Styles()],
            [new Colors(), new Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkPurpleThemeGuid,
            "ThemesService.Themes.DarkPurple",
            ThemeVariant.Dark,
            [new Themes.DarkPurple.Styles()],
            [new Themes.DarkPurple.Colors(), new Themes.DarkPurple.Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.DarkGreenThemeGuid,
            "ThemesService.Themes.DarkGreen",
            ThemeVariant.Dark,
            [new Themes.DarkGreen.Styles()],
            [new Themes.DarkGreen.Colors(), new Themes.DarkGreen.Resources()]));

        ServiceCollection.AddSingleton(new RegisteredTheme(
            Constants.LightThemeGuid,
            "ThemesService.Themes.Light",
            ThemeVariant.Light,
            [new Themes.Light.Styles()],
            [new Themes.Light.Colors(), new Themes.Light.Resources()]));

        //Background Image Registry
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.DockNightGuid,
            "ThemesService.BackgroundImages.DockNight",
            "avares://VoiceCraft.Client/Assets/bgdark.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.DockDayGuid,
            "ThemesService.BackgroundImages.DockDay",
            "avares://VoiceCraft.Client/Assets/bglight.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.LethalCraftGuid,
            "ThemesService.BackgroundImages.LethalCraft",
            "avares://VoiceCraft.Client/Assets/lethalCraft.png"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.BlockSenseSpawnGuid,
            "ThemesService.BackgroundImages.BlockSenseSpawn",
            "avares://VoiceCraft.Client/Assets/blocksensespawn.jpg"));
        ServiceCollection.AddSingleton(new RegisteredBackgroundImage(
            Constants.SineSmpBaseGuid,
            "ThemesService.BackgroundImages.SineSmpBase",
            "avares://VoiceCraft.Client/Assets/sinesmpbase.png"));

        //HotKey Registry
        ServiceCollection.AddSingleton<HotKeyAction, MuteAction>();
        ServiceCollection.AddSingleton<HotKeyAction, DeafenAction>();
        ServiceCollection.AddSingleton<HotKeyAction, PushToTalkAction>();

        //Audio Registry
        ServiceCollection.AddSingleton<AudioService>(x =>
            new AudioService(
                x.GetRequiredService<AudioEngine>(),
                x.GetServices<RegisteredAudioPreprocessor>(),
                x.GetServices<RegisteredAudioClipper>()
            ));
        ServiceCollection.AddTransient<IAudioEncoder, OpusAudioEncoder>();
        ServiceCollection.AddTransient<IAudioDecoder, OpusAudioDecoder>();
        ServiceCollection.AddSingleton(new RegisteredAudioClipper(
            Constants.HardAudioClipperGuid,
            "AudioService.Clippers.Hard",
            () => new SampleHardAudioClipper()));
        ServiceCollection.AddSingleton(new RegisteredAudioClipper(
            Constants.TanhSoftAudioClipperGuid,
            "AudioService.Clippers.TanhSoft",
            () => new SampleTanhSoftAudioClipper()));

        return ServiceCollection.BuildServiceProvider();
    }

    private void SetupServices(IServiceProvider serviceProvider)
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales");
        DataTemplates.Add(serviceProvider.GetRequiredService<ViewLocatorService>());
    }

    private static void RegisterClipboardWhenAttached(IServiceProvider serviceProvider, Control control)
    {
        control.AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(control) is { } topLevel)
                serviceProvider.GetRequiredService<ClipboardService>().RegisterTopLevel(topLevel);
        };
    }

    private static void ConfigureTelemetry(IServiceProvider serviceProvider)
    {
        TelemetryTransport.FailureLogger = LogService.LogInfo;

        var settingsService = serviceProvider.GetRequiredService<SettingsService>();
        ClientTelemetry.SetTelemetryToken(settingsService.TelemetryToken);
        if (settingsService.TelemetrySettings.Enabled && settingsService.TelemetrySettings.ConsentShown)
            _ = ClientTelemetry.ReportStartupAsync(settingsService, 3);
    }
}

