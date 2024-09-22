using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.Views;
using Avalonia.Notification;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<NotificationMessageManager>();
            serviceCollection.AddSingleton<SettingsService>();
            serviceCollection.AddSingleton<ThemesService>();
            PluginLoader.LoadPlugins($"{AppContext.BaseDirectory}/Plugins", serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            PluginLoader.InitializePlugins(serviceProvider);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var mainViewModel = serviceProvider.GetRequiredService<IMainView>();
                desktop.MainWindow = new MainWindow
                {
                    Content = mainViewModel,
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                var mainView = serviceProvider.GetRequiredService<IMainView>();
                singleViewPlatform.MainView = (Control)mainView;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}