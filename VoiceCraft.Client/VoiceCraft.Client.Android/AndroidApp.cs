using System;
using Android.Media;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using SoundFlow.Abstracts;
using VoiceCraft.Client.Android.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.Clients;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;

namespace VoiceCraft.Client.Android;

[global::Android.App.Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
        
    }

    public override void OnCreate()
    {
        BootstrapServices();
        base.OnCreate();
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (App.ServiceProvider == null) return;
            var serviceProvider = App.ServiceProvider;
            serviceProvider.Dispose();
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    private static void BootstrapServices()
    {
        var nativeStorage = new NativeStorageService();
        LogService.NativeStorageService = nativeStorage;
        LogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        var audioManager = (AudioManager?)Context.GetSystemService(AudioService);
        if (audioManager == null)
            throw new Exception($"Could not find {AudioService}. Cannot initialize audio service.");

        App.ServiceCollection.AddSingleton<AudioEngine, AndroidMiniAudioEngine>(_ =>
            new AndroidMiniAudioEngine(audioManager));
        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<HotKeyService, NativeHotKeyService>();
        App.ServiceCollection.AddSingleton<IBackgroundService>(x =>
            new NativeBackgroundService(x.GetRequiredService<PermissionsService>(), x.GetRequiredService));
        App.ServiceCollection.AddSingleton<RegisteredAudioPreprocessor>(_ =>
            new RegisteredAudioPreprocessor(
                Constants.SpeexDspPreprocessorGuid,
                "AudioService.Preprocessors.Speex",
                () => new SpeexDspPreprocessor(
                    Constants.SampleRate,
                    Constants.FrameSize,
                    Constants.RecordingChannels,
                    Constants.PlaybackChannels),
                true,
                true,
                true));
        App.ServiceCollection.AddTransient<VoiceCraftClient>(x =>
            new LiteNetVoiceCraftClient(
                x.GetRequiredService<IAudioEncoder>(),
                x.GetRequiredService<IAudioDecoder>));
        App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
        App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone>();
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                LogService.LogCrash(ex);
        }
        catch (Exception writeEx)
        {
            Debug.WriteLine(writeEx);
        }
    }
}
