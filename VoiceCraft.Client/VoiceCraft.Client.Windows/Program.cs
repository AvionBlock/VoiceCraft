using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK;

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
            
            //Register Native Players. TODO - THIS ONLY APPLIES TO WINDOWS, WE NEED TO SEPARATE THE DESKTOP PROJECT INTO 3 PLATFORM PROJECTS.
            App.Services.AddSingleton<IWaveIn, WaveInEvent>();
            App.Services.AddSingleton<IWavePlayer, WaveOutEvent>();
            App.Services.AddSingleton<IAudioDevices, AudioDevices>();
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
