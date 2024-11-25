using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.Windows.Audio;

namespace VoiceCraft.Client.Windows
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var avaloniaApp = BuildAvaloniaApp();

            App.Services.AddSingleton<PermissionsService>();
            App.Services.AddSingleton<AudioService, NativeAudioService>();
            avaloniaApp.StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
