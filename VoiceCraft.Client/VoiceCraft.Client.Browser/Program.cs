using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Browser.Audio;
using VoiceCraft.Client.Browser.Permissions;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser
{
    sealed class Program
    {
        private static async Task Main(string[] _)
        {
            await BuildJsInterops();
            
            var nativeStorage = new NativeStorageService();
            CrashLogService.NativeStorageService = nativeStorage;
            CrashLogService.Load();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>();
            
            App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
            App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
            App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Microphone>();
            
            await BuildAvaloniaApp()
                .WithInterFont()
                .StartBrowserAppAsync("out");
        }

        public static AppBuilder BuildAvaloniaApp() => 
            AppBuilder.Configure<App>();
        
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

        private static async Task BuildJsInterops()
        {
            await JSHost.ImportAsync("audio.js", "/Exports/audio.js");
            await JSHost.ImportAsync("microphonePermission.js", "/Exports/microphonePermission.js");
            await JSHost.ImportAsync("storage.js", "/Exports/storage.js");
        }
    }
}