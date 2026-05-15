using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Microsoft.Extensions.DependencyInjection;
using SoundFlow.Abstracts;
using VoiceCraft.Client.Browser.Audio;
using VoiceCraft.Client.Browser.Permissions;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.Clients;

namespace VoiceCraft.Client.Browser;

internal sealed class Program
{
    private static async Task Main(string[] _)
    {
        await BuildJsInterops();

        var nativeStorage = new NativeStorageService();
        LogService.NativeStorageService = nativeStorage;
        LogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        
        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<HotKeyService, NativeHotKeyService>();
        App.ServiceCollection.AddSingleton<IBackgroundService>(x => new NativeBackgroundService(x.GetRequiredService));
        App.ServiceCollection.AddSingleton<AudioEngine, BrowserMiniAudioEngine>();
        App.ServiceCollection.AddTransient<VoiceCraftClient>(x =>
            new WebRtcVoiceCraftClient(
                x.GetRequiredService<IAudioEncoder>(),
                x.GetRequiredService<IAudioDecoder>));
        App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Microphone>();

        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                LogService.LogCrash(ex); //Log it
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
        await JSHost.ImportAsync("webrtc.js", "/Exports/webrtc.js");
    }
}
