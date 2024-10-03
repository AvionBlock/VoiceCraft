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
using VoiceCraft.Client.ViewModels;
using System.Diagnostics;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public static ServiceCollection Services { get; } = new ServiceCollection();
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Services.AddSingleton<NavigationService>(s => new NavigationService(p => (Control)s.GetRequiredService(p)));
            Services.AddSingleton<NotificationMessageManager>();
            Services.AddSingleton<SettingsService>();
            Services.AddSingleton<ThemesService>();

            IMainView? mainView = null;
            ServiceProvider? serviceProvider = null;
            try
            {
                PluginLoader.LoadPlugins($"{AppContext.BaseDirectory}/Plugins", Services);

                serviceProvider = Services.BuildServiceProvider();
                mainView = serviceProvider.GetRequiredService<IMainView>();
            }
            catch (Exception ex)
            {
                mainView = new DefaultMainView(new DefaultMainViewModel() { Message = $"Error: {ex.Message}" });
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                desktop.MainWindow = new MainWindow
                {
                    Content = mainView,
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = (Control)mainView;
            }

            if(serviceProvider != null)
                PluginLoader.InitializePlugins(serviceProvider);

            base.OnFrameworkInitializationCompleted();
        }
    }
}