using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.Views;

namespace VoiceCraft.Client;

public class App : Application
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    public static readonly Guid SpeexDspPreprocessorGuid = Guid.Parse("6a9fba40-453d-4943-bebc-82963c8397ae");
    public static readonly Guid SpeexDspEchoCancelerGuid = Guid.Parse("b4844eca-d5c0-497a-9819-7e4fa9ffa7ed");
    public static readonly Guid DarkThemeGuid = Guid.Parse("cf8e39fe-21cc-4210-91e6-d206e22ca52e");
    public static readonly Guid LightThemeGuid = Guid.Parse("3aeb95bc-a749-40f0-8f45-9f9070b76125");
    public static readonly Guid DockNightGuid = Guid.Parse("6b023e19-c9c5-4e06-84df-22833ccccd87");
    public static readonly Guid DockDayGuid = Guid.Parse("7c615c28-33b7-4d1d-b530-f8d988b00ea1");
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
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

                desktop.MainWindow.Closing += (__, ___) =>
                {
                    _ = serviceProvider.GetRequiredService<SettingsService>().SaveImmediate();
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                };
                break;
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
        ServiceCollection.AddSingleton<NavigationService>(x => new NavigationService(y => (ViewModelBase)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<INotificationMessageManager, NotificationMessageManager>();
        ServiceCollection.AddSingleton<NotificationService>();
        ServiceCollection.AddSingleton<PermissionsService>();
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
        
        return ServiceCollection.BuildServiceProvider();
    }

    private static void SetupServices(IServiceProvider serviceProvider)
    {
        var audioService = serviceProvider.GetRequiredService<AudioService>();
        audioService.RegisterPreprocessor<SpeexDspPreprocessor>(SpeexDspPreprocessorGuid, "SpeexDSP Preprocessor");
        audioService.RegisterEchoCanceler<SpeexDspEchoCanceler>(SpeexDspEchoCancelerGuid, "SpeexDSP Echo Canceler");
        
        var themesService = serviceProvider.GetRequiredService<ThemesService>();
        themesService.RegisterTheme(DarkThemeGuid, "Dark", [new Themes.Dark.Styles()], [new Themes.Dark.VcColors(), new Themes.Dark.Resources()], ThemeVariant.Dark);
        themesService.RegisterTheme(LightThemeGuid, "Light", [new Themes.Light.Styles()], [new Themes.Light.Colors(), new Themes.Light.Resources()], ThemeVariant.Light);
        themesService.RegisterBackgroundImage(DockNightGuid, "Dock Night", "avares://VoiceCraft.Client/Assets/bgdark.png");
        themesService.RegisterBackgroundImage(DockDayGuid, "Dock Day", "avares://VoiceCraft.Client/Assets/bglight.png");
    }

    private static SettingsService SetupSettings()
    {
        var settings = new SettingsService();
        settings.RegisterSetting<AudioSettings>();
        settings.RegisterSetting<NotificationSettings>();
        settings.RegisterSetting<ServersSettings>();
        settings.RegisterSetting<ThemeSettings>();
        settings.Load();
        return settings;
    }
}