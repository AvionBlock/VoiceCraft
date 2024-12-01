using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Settings;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Home;
using VoiceCraft.Client.Views;

namespace VoiceCraft.Client;

public partial class App : Application
{
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceProvider = BuildServiceProvider();
        SetupSettings(serviceProvider);
        
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

    private void DisableAvaloniaDataAnnotationValidation()
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

    private IServiceProvider BuildServiceProvider()
    {
        //Service Registry
        ServiceCollection.AddSingleton<HistoryRouter<ViewModelBase>>(x => new HistoryRouter<ViewModelBase>(y => (ViewModelBase)x.GetRequiredService(y)));
        ServiceCollection.AddSingleton<INotificationMessageManager, NotificationMessageManager>();
        ServiceCollection.AddSingleton<NotificationService>();
        ServiceCollection.AddSingleton<ThemesService>();
        ServiceCollection.AddSingleton<SettingsService>();
        
        //Pages Registry
        ServiceCollection.AddSingleton<MainViewModel>();
        
        //Main Pages
        ServiceCollection.AddTransient<HomeViewModel>();
        ServiceCollection.AddSingleton<EditServerViewModel>();
        ServiceCollection.AddSingleton<AddServerViewModel>();
        
        //Home Pages
        ServiceCollection.AddTransient<ServersViewModel>();
        ServiceCollection.AddTransient<SettingsViewModel>();
        ServiceCollection.AddSingleton<ServerViewModel>();
        ServiceCollection.AddSingleton<CreditsViewModel>();
        
        return ServiceCollection.BuildServiceProvider();
    }

    private void SetupSettings(IServiceProvider services)
    {
        var settings = services.GetRequiredService<SettingsService>();
        settings.RegisterSetting<AudioSettings>();
        settings.RegisterSetting<NotificationSettings>();
        settings.RegisterSetting<ServersSettings>();
        settings.RegisterSetting<ThemeSettings>();
    }
}