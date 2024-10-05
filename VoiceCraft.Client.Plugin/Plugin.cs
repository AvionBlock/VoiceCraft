using Avalonia.Markup.Xaml.Styling;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.ViewModels;
using VoiceCraft.Client.Plugin.ViewModels.Home;
using VoiceCraft.Client.Plugin.Views;
using VoiceCraft.Client.Plugin.Views.Home;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Plugin
{
    public class Plugin : IPlugin
    {
        public static Guid PluginId => Guid.Empty;
        public Guid Id => PluginId;
        public string Name => "VoiceCraft";
        public string Description => "The main voicecraft plugin.";

        public int Priority => 0;
        public IEnumerable<Guid> ClientDependencies => [];
        public IEnumerable<Guid> ServerDependencies => [PluginId];

        public void Load(ServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IMainView, MainView>();
            serviceCollection.AddScoped<IMainViewModel, MainViewModel>();

            //Pages
            serviceCollection.AddTransient<HomeViewModel>();
            serviceCollection.AddTransient<ServersViewModel>();
            serviceCollection.AddTransient<ServerViewModel>();
            serviceCollection.AddTransient<AddServerViewModel>();
            serviceCollection.AddTransient<PluginsViewModel>();
            serviceCollection.AddTransient<EditServerViewModel>();
            serviceCollection.AddTransient<SettingsViewModel>();
            serviceCollection.AddTransient<CreditsViewModel>();

            serviceCollection.AddTransient<HomeView>();
            serviceCollection.AddTransient<ServersView>();
            serviceCollection.AddTransient<ServerView>();
            serviceCollection.AddTransient<AddServerView>();
            serviceCollection.AddTransient<PluginsView>();
            serviceCollection.AddTransient<EditServerView>();
            serviceCollection.AddTransient<SettingsView>();
            serviceCollection.AddTransient<CreditsView>();
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            var settings = serviceProvider.GetRequiredService<SettingsService>();
            settings.RegisterSetting<ServersSettings>(PluginId);
            settings.RegisterSetting<AudioSettings>(PluginId);
            settings.RegisterSetting<ThemeSettings>(PluginId);
            settings.RegisterSetting<NotificationSettings>(PluginId);
            settings.Load();

            var themes = serviceProvider.GetRequiredService<ThemesService>();
            var themeSettings = settings.Get<ThemeSettings>(PluginId);
            themes.RegisterTheme("Light", Avalonia.Platform.PlatformThemeVariant.Light,
                new StyleInclude(new Uri(@"avares://VoiceCraft.Client.Plugin")) { Source = new Uri(@"/Assets/Styles.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://VoiceCraft.Client.Plugin")) { Source = new Uri(@"/Assets/Icons.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://Notification.Avalonia")) { Source = new Uri(@"/Themes/Generic.xaml", UriKind.Relative) });
            themes.RegisterTheme("Dark", Avalonia.Platform.PlatformThemeVariant.Dark,
                new StyleInclude(new Uri(@"avares://VoiceCraft.Client.Plugin")) { Source = new Uri(@"/Assets/Styles.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://VoiceCraft.Client.Plugin")) { Source = new Uri(@"/Assets/Icons.axaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://Notification.Avalonia")) { Source = new Uri(@"/Themes/Generic.xaml", UriKind.Relative) });

            themes.SwitchTheme(themeSettings.SelectedTheme);

            var navigation = serviceProvider.GetRequiredService<NavigationService>();
            navigation.NavigateTo<HomeView>();
        }
    }
}