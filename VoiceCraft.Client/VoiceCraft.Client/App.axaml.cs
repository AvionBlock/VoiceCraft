using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
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
            services.AddTransient<RefreshingViewModel>();
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
            themes.RegisterTheme("Fluent", new Theme(new FluentTheme(), new ThemeVariant("Default", null), new ThemeVariant("Dark", null), new ThemeVariant("Light", null)));
            themes.RegisterTheme("Simple", new Theme(new SimpleTheme(), new ThemeVariant("Default", null), new ThemeVariant("Dark", null), new ThemeVariant("Light", null)));
            var themeSetting = settings.Get<ThemeSettings>(SettingsId);

            themes.OnThemeChanged += (from, to) =>
            {
                if (Current == null) return;
                Current.Styles.Clear();

                Current.Styles.Add(to.ThemeStyle);
                Current.RequestedThemeVariant = new ThemeVariant("Default", null);
            };
        }
    }
}