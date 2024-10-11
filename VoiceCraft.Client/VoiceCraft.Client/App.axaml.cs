using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.Views;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public static ServiceCollection Services { get; } = new ServiceCollection();
        public static readonly string PluginDirectory = $"{AppContext.BaseDirectory}/Plugins";
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Services.AddSingleton<NavigationService>(s => new NavigationService(p => (ViewBase)s.GetRequiredService(p)));
            Services.AddSingleton<NotificationMessageManager>();
            Services.AddSingleton<SettingsService>();
            Services.AddSingleton<ThemesService>();

            ServiceProvider? serviceProvider = null;
            PluginLoader.LoadPlugins(PluginDirectory, Services);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var mainWindow = new MainWindow();
                IMainView mainView;
                try
                {
                    Services.AddSingleton(mainWindow.StorageProvider);
                    serviceProvider = Services.BuildServiceProvider();

                    mainView = serviceProvider.GetRequiredService<IMainView>();
                }
                catch (Exception ex)
                {
                    mainView = new DefaultMainView(new DefaultMainViewModel() { Message = $"Error: {ex.Message}" });
                }

                mainWindow.Content = mainView;
                desktop.MainWindow = mainWindow;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                var topLevel = (TopLevel?)ApplicationLifetime.GetType()?.GetProperty(nameof(TopLevel))?.GetValue(ApplicationLifetime, null);
                IMainView mainView;
                try
                {
                    if (topLevel == null)
                        throw new Exception("Could not find visual root!");

                    Services.AddSingleton(topLevel.StorageProvider);
                    serviceProvider = Services.BuildServiceProvider();

                    mainView = serviceProvider.GetRequiredService<IMainView>();
                }
                catch (Exception ex)
                {
                    mainView = new DefaultMainView(new DefaultMainViewModel() { Message = $"Error: {ex.Message}" });
                }

                singleViewPlatform.MainView = (Control)mainView;
            }

            if (serviceProvider != null)
                PluginLoader.InitializePlugins(serviceProvider);

            base.OnFrameworkInitializationCompleted();
        }
    }
}