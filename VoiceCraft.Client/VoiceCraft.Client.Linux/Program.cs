using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Linux.Audio;
using VoiceCraft.Client.Linux.Permissions;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux
{
    sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>();
            App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
            App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Microphone>();
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}