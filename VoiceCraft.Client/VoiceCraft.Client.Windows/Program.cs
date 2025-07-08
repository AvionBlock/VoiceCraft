using System;
using System.Diagnostics;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Windows.Audio;
using VoiceCraft.Client.Windows.Permissions;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Windows;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var nativeStorage = new NativeStorageService();
        LogService.NativeStorageService = nativeStorage;
        LogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        
        App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>();

        //Register Speex Preprocessors
        App.ServiceCollection.AddSingleton(new RegisteredEchoCanceler(
            Constants.SpeexDspEchoCancelerGuid,
            "SpeexDsp Echo Canceler",
            typeof(SpeexDspEchoCanceler)));
        App.ServiceCollection.AddSingleton(new RegisteredAutomaticGainController(
            Constants.SpeexDspAutomaticGainControllerGuid,
            "SpeexDsp Automatic Gain Controller",
            typeof(SpeexDspAutomaticGainController)));
        App.ServiceCollection.AddSingleton(new RegisteredDenoiser(
            Constants.SpeexDspDenoiserGuid,
            "SpeexDsp Denoiser",
            typeof(SpeexDspDenoiser)));

        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
        App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Microphone>();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
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
}