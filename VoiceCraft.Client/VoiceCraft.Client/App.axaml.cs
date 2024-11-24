using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.Views;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public static ServiceCollection Services { get; } = new ServiceCollection();
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static readonly string PluginDirectory = $"{AppContext.BaseDirectory}/Plugins";
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Services.AddSingleton<NavigationService>(s => new NavigationService(p => (ViewModelBase)s.GetRequiredService(p)));
            Services.AddSingleton<NotificationMessageManager>();
            Services.AddSingleton<SettingsService>();
            Services.AddSingleton<ThemesService>();

            PluginLoader.LoadPlugins(PluginDirectory, Services);

            IMainView mainView;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                var mainWindow = new MainWindow();
                try
                {
                    Services.AddSingleton<TopLevel>(mainWindow);
                    ServiceProvider = Services.BuildServiceProvider();

                    DataTemplates.Add(new ViewLocator(ServiceProvider));
                    mainView = ServiceProvider.GetRequiredService<IMainView>();
                }
                catch (Exception ex)
                {
                    mainView = new DefaultMainView() { DataContext = new DefaultMainViewModel() { Message = $"Error: {ex.Message}" } };
                }

                mainWindow.Content = mainView;
                desktop.MainWindow = mainWindow;

                desktop.Exit += ApplicationExit;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                //This is stupid but this is the only way I can find out how to get top level on single view platform... Why is this not part of the interface...
                var topLevel = (TopLevel?)ApplicationLifetime.GetType()?.GetProperty(nameof(TopLevel))?.GetValue(ApplicationLifetime, null);
                try
                {
                    if (topLevel == null)
                        throw new Exception("Could not find visual root!");

                    Services.AddSingleton(topLevel);
                    ServiceProvider = Services.BuildServiceProvider();

                    DataTemplates.Add(new ViewLocator(ServiceProvider));
                    mainView = ServiceProvider.GetRequiredService<IMainView>();
                }
                catch (Exception ex)
                {
                    mainView = new DefaultMainView() { DataContext = new DefaultMainViewModel() { Message = $"Error: {ex.Message}" } };
                }

                singleViewPlatform.MainView = (Control)mainView;
            }

            if (ServiceProvider != null)
                PluginLoader.InitializePlugins(ServiceProvider);

            base.OnFrameworkInitializationCompleted();
        }

        private async void ApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            var settings = ServiceProvider?.GetService<SettingsService>();
            if (settings != null)
            {
                await settings.SaveImmediate();
            }
        }
    }
}