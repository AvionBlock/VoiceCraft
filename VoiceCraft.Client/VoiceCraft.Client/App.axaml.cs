using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using Avalonia.Styling;
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
        private static IServiceProvider? Services;

        public override void Initialize()
        {
            Services = ConfigureServices();
            ConfigureApplicationServices(Services);

            //Initialize All Plugins
            AvaloniaXamlLoader.Load(this);
        }

        public unsafe override void OnFrameworkInitializationCompleted()
        {
            if(Services == null)
                throw new NullReferenceException($"{nameof(Services)} was not created!");

            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
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

        private ServiceProvider ConfigureServices()
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

        private void ConfigureApplicationServices(IServiceProvider services)
        {
            var settings = services.GetRequiredService<SettingsService>();
            settings.RegisterSetting<ServersSettings>(SettingsId);
            settings.RegisterSetting<AudioSettings>(SettingsId);
            settings.RegisterSetting<ThemeSettings>(SettingsId);
            settings.Load();

            var themes = services.GetRequiredService<ThemesService>();
            var themeSettings = settings.Get<ThemeSettings>(SettingsId);
            themes.RegisterTheme("Light", Avalonia.Platform.PlatformThemeVariant.Light,
                new StyleInclude(new Uri(@"avares://VoiceCraft.Core")) { Source = new Uri(@"/Assets/Styles.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://VoiceCraft.Core")) { Source = new Uri(@"/Assets/Icons.axaml", UriKind.Relative) });
            themes.RegisterTheme("Dark", Avalonia.Platform.PlatformThemeVariant.Dark,
                new StyleInclude(new Uri(@"avares://VoiceCraft.Core")) { Source = new Uri(@"/Assets/Styles.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://VoiceCraft.Core")) { Source = new Uri(@"/Assets/Icons.axaml", UriKind.Relative) });

            themes.SwitchTheme(themeSettings.SelectedTheme);
        }
    }
}