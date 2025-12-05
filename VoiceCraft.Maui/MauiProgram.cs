using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SimpleToolkit.Core;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSimpleToolkit()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Services
            builder.Services.AddSingleton<IDatabaseService, Database>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IAudioManager, AudioManager>();

            // ViewModels
            builder.Services.AddSingleton<ServersViewModel>();
            builder.Services.AddTransient<AddServerViewModel>();
            builder.Services.AddTransient<EditServerViewModel>();
            builder.Services.AddTransient<ServerDetailsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<VoiceViewModel>();

            // Views - Desktop
            builder.Services.AddSingleton<Views.Desktop.Servers>();
            builder.Services.AddTransient<Views.Desktop.AddServer>();
            builder.Services.AddTransient<Views.Desktop.EditServer>();
            builder.Services.AddTransient<Views.Desktop.ServerDetails>();
            builder.Services.AddTransient<Views.Desktop.Settings>();
            builder.Services.AddTransient<Views.Desktop.Credits>();
            builder.Services.AddTransient<Views.Desktop.Voice>();

            // Views - Mobile
            builder.Services.AddSingleton<Views.Mobile.Servers>();
            builder.Services.AddTransient<Views.Mobile.AddServer>();
            builder.Services.AddTransient<Views.Mobile.EditServer>();
            builder.Services.AddTransient<Views.Mobile.ServerDetails>();
            builder.Services.AddTransient<Views.Mobile.Settings>();
            builder.Services.AddTransient<Views.Mobile.Credits>();
            builder.Services.AddTransient<Views.Mobile.Voice>();

            // App & Shell
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();

            return builder.Build();
        }
    }
}
