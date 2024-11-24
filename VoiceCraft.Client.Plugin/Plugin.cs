using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using DialogHostAvalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.Plugin.Assets;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.ViewModels;
using VoiceCraft.Client.Plugin.ViewModels.Home;
using VoiceCraft.Client.Plugin.Views;
using VoiceCraft.Client.Plugin.Views.Home;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Plugin
{
    
    [Plugin("00000000-0000-0000-0000-000000000000", "VoiceCraft", "The main voicecraft plugin.", 0, [], ["00000000-0000-0000-0000-000000000000"])]
    public class Plugin : IPlugin
    {
        public static Guid PluginId => Guid.Empty;

        public void Load(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMainView, MainView>();
            serviceCollection.AddSingleton<IMainViewModel, MainViewModel>();

            //Pages
            serviceCollection.AddTransient<HomeViewModel>();
            serviceCollection.AddTransient<ServersViewModel>();
            serviceCollection.AddTransient<ServerViewModel>();
            serviceCollection.AddTransient<AddServerViewModel>();
            serviceCollection.AddTransient<PluginsViewModel>();
            serviceCollection.AddTransient<EditServerViewModel>();
            serviceCollection.AddTransient<SettingsViewModel>();
            serviceCollection.AddTransient<CreditsViewModel>();

            serviceCollection.AddKeyedTransient<Control, HomeView>(typeof(HomeView).FullName);
            serviceCollection.AddKeyedTransient<Control, ServersView>(typeof(ServersView).FullName);
            serviceCollection.AddKeyedTransient<Control, ServerView>(typeof(ServerView).FullName);
            serviceCollection.AddKeyedTransient<Control, AddServerView>(typeof(AddServerView).FullName);
            serviceCollection.AddKeyedTransient<Control, PluginsView>(typeof(PluginsView).FullName);
            serviceCollection.AddKeyedTransient<Control, EditServerView>(typeof(EditServerView).FullName);
            serviceCollection.AddKeyedTransient<Control, SettingsView>(typeof(SettingsView).FullName);
            serviceCollection.AddKeyedTransient<Control, CreditsView>(typeof(CreditsView).FullName);
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
                new Icons(),
                new StyleInclude(new Uri(@"avares://Notification.Avalonia")) { Source = new Uri(@"/Themes/Generic.xaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://DialogHost.Avalonia")) { Source = new Uri(@"/Styles.xaml", UriKind.Relative) });
            themes.RegisterTheme("Dark", Avalonia.Platform.PlatformThemeVariant.Dark,
                new StyleInclude(new Uri(@"avares://VoiceCraft.Client.Plugin")) { Source = new Uri(@"/Assets/Styles.axaml", UriKind.Relative) },
                new Icons(),
                new StyleInclude(new Uri(@"avares://Notification.Avalonia")) { Source = new Uri(@"/Themes/Generic.xaml", UriKind.Relative) },
                new StyleInclude(new Uri(@"avares://DialogHost.Avalonia")) { Source = new Uri(@"/Styles.xaml", UriKind.Relative) });

            themes.SwitchTheme(themeSettings.SelectedTheme);

            var navigation = serviceProvider.GetRequiredService<NavigationService>();
            navigation.NavigateTo<HomeViewModel>();
        }
    }
}