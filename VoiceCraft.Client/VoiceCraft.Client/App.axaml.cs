using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
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
        ServiceCollection.AddSingleton<SelectedServerViewModel>();
        ServiceCollection.AddSingleton<CreditsViewModel>();
        
        return ServiceCollection.BuildServiceProvider();
    }

    private static void SetupServices(IServiceProvider serviceProvider)
    {
        var audioService = serviceProvider.GetRequiredService<AudioService>();
        audioService.RegisterPreprocessor<SpeexDspPreprocessor>(Guid.Parse("6a9fba40-453d-4943-bebc-82963c8397ae"), "SpeexDSP Preprocessor");
        audioService.RegisterEchoCanceler<SpeexDspEchoCanceler>(Guid.Parse("b4844eca-d5c0-497a-9819-7e4fa9ffa7ed"), "SpeexDSP Echo Canceler");
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