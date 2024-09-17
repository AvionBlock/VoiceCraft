using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.HomeViews;
using VoiceCraft.Client.Views;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;
using VoiceCraft.Core.Settings;

namespace VoiceCraft.Client
{
    public partial class App : Application
    {
        public static readonly Guid SettingsId = Guid.Empty;
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public unsafe override void OnFrameworkInitializationCompleted()
        {
            IServiceProvider services = ConfigureServices();

            //Initialize All Plugins

            ConfigureApplicationServices(services);

            var mainViewModel = services.GetRequiredService<MainViewModel>();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = mainViewModel,
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
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

            services.AddSingleton<NotificationMessageManager>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ThemesService>();

            //Main stuff.
            services.AddSingleton<MainViewModel>();

            //Pages
            services.AddTransient<HomeViewModel>();
            services.AddTransient<ServersViewModel>();
            services.AddTransient<SettingsViewModel>();

            //Build Plugin Services.

            return services.BuildServiceProvider();
        }

        private static void ConfigureApplicationServices(IServiceProvider services)
        {
            var settings = services.GetRequiredService<SettingsService>();
            settings.RegisterSetting<ServersSettings>(SettingsId);
            settings.RegisterSetting<AudioSettings>(SettingsId);
            settings.RegisterSetting<ThemeSettings>(SettingsId);
            settings.Load();

            var themes = services.GetRequiredService<ThemesService>();
            themes.RegisterTheme(new FluentTheme(), "Default", "Light", "Dark");

            foreach(var theme in themes.Themes)
            {
                Current?.Styles.Add(theme);
            }

            var themeSetting = settings.Get<ThemeSettings>(SettingsId);

            if (Current?.RequestedThemeVariant != null)
            {
                Current.RequestedThemeVariant = new Avalonia.Styling.ThemeVariant(themeSetting.SelectedTheme, null);
            }
        }
    }
}