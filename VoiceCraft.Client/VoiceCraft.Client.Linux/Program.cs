using System;
using System.Diagnostics;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Linux.Audio;
using VoiceCraft.Client.Linux.Permissions;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;

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
                CrashLogService.Load();
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                App.ServiceCollection.AddSingleton<AudioService>(_ =>
                {
                    var audioService = new NativeAudioService();
                    audioService.RegisterEchoCanceler<SpeexDspEchoCanceler>(Constants.SpeexDspEchoCancelerGuid, "SpeexDsp Echo Canceler");
                    audioService.RegisterAutomaticGainController<SpeexDspAutomaticGainController>(Constants.SpeexDspAutomaticGainControllerGuid, "SpeexDsp Automatic Gain Controller");
                    audioService.RegisterDenoiser<SpeexDspDenoiser>(Constants.SpeexDspDenoiserGuid, "SpeexDsp Denoiser");
                
                    return audioService;
                });
                
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
        
        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                    CrashLogService.Log(ex); //Log it
            }
            catch (Exception writeEx)
            {
                Debug.WriteLine(writeEx); //We don't want to crash if the log failed.
            }
        }
    }
}