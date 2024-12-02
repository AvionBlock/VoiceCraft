using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Linux.Audio;
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