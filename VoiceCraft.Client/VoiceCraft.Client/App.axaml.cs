using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.Views;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;

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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var mainViewModel = services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = new MainWindow();
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

            services.AddSingleton<NotificationMessageManager>();
            services.AddSingleton<NavigationService<ViewModelBase>>();
            services.AddSingleton<SettingsService>();

            //Main stuff.
            services.AddSingleton<MainViewModel>();
            return services.BuildServiceProvider();
        }
    }
}