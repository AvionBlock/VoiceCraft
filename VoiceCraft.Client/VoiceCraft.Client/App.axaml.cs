using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.HomeViews;
using VoiceCraft.Client.Views;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public unsafe override void OnFrameworkInitializationCompleted()
        {
            IServiceProvider services = ConfigureServices();
            var settings = services.GetRequiredService<SettingsModel>();
            settings.Load();
            RequestedThemeVariant = new Avalonia.Styling.ThemeVariant(settings.SelectedTheme, null);
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var mainViewModel = services.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                var mainViewModel = services.GetRequiredService<MainViewModel>();
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = mainViewModel
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<HistoryRouter<ViewModelBase>>(s => new HistoryRouter<ViewModelBase>(t => (ViewModelBase)s.GetRequiredService(t)));
            
            //Main stuff.
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainViewModel>();

            services.AddSingleton<NotificationMessageManager>();

            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<ServersViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<CreditsViewModel>();
            services.AddSingleton<ServerViewModel>();
            services.AddSingleton<EditServerViewModel>();
            services.AddSingleton<AddServerViewModel>();
            services.AddSingleton<SettingsModel>();
            return services.BuildServiceProvider();
        }
    }
}