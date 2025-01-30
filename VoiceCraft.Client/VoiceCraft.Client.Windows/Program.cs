using System;
using System.Diagnostics;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Windows.Audio;

namespace VoiceCraft.Client.Windows
{
    sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                CrashLogService.Load();
                App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>();
                App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
                App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Permissions.Microphone>();

                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                try
                {
                    CrashLogService.Log(ex); //Log it
                }
                catch (Exception writeEx)
                {
                    Debug.WriteLine(writeEx); //We don't want to crash if the log failed.
                }
                throw; //rethrow so if logging fails, the system event handler should catch it.
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}