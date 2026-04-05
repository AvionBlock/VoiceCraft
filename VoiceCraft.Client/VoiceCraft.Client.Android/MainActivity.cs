using System;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using AndroidX.Activity;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using VoiceCraft.Client.Android.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.Clients;
using Debug = System.Diagnostics.Debug;
using Exception = System.Exception;

namespace VoiceCraft.Client.Android;

[Activity(
    Label = "VoiceCraft",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
        Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnCreate(Bundle? app)
    {
        var nativeStorage = new NativeStorageService();
        LogService.NativeStorageService = nativeStorage;
        LogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(this));

        var audioManager = (AudioManager?)GetSystemService(AudioService);
        if (audioManager == null)
            throw new Exception($"Could not find {AudioService}. Cannot initialize audio service.");

        App.ServiceCollection.AddSingleton<AudioEngine, MiniAudioEngine>();
        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<HotKeyService, NativeHotKeyService>();
        App.ServiceCollection.AddSingleton<IBackgroundService>(x =>
            new NativeBackgroundService(x.GetRequiredService<PermissionsService>(), x.GetRequiredService));
        App.ServiceCollection.AddSingleton<RegisteredAudioPreprocessor>(_ =>
            new RegisteredAudioPreprocessor(
                Constants.SpeexDspPreprocessorGuid, 
                "Speex",
                () => new SpeexDspPreprocessor(
                    Constants.SampleRate,
                    Constants.FrameSize,
                    Constants.RecordingChannels,
                    Constants.PlaybackChannels),
                true,
                true,
                true));
        App.ServiceCollection.AddTransient<VoiceCraftClient>(x =>
            new LiteNetVoiceCraftClient(x.GetRequiredService<IAudioEncoder>(),
                x.GetRequiredService<IAudioDecoder>));
        App.ServiceCollection.AddTransient<Permissions.PostNotifications>();
        App.ServiceCollection.AddTransient<Permissions.Microphone>();

        Platform.Init(this, app);
        base.OnCreate(app);
    }

    protected override void OnDestroy()
    {
        try
        {
            if (App.ServiceProvider == null) return;
            var serviceProvider = App.ServiceProvider;
            serviceProvider.Dispose();
        }
        finally
        {
            base.OnDestroy();
        }
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

    private static bool BackButtonBehavior()
    {
        if (App.ServiceProvider == null) return false;
        var navigationService = App.ServiceProvider.GetService<NavigationService>();
        return navigationService?.Back(true) != null;
    }

    private class BackPressedCallback(MainActivity activity, bool enabled = true) : OnBackPressedCallback(enabled)
    {
        public override void HandleOnBackPressed()
        {
            if (BackButtonBehavior()) return;
            activity.FinishAndRemoveTask();
            Process.KillProcess(Process.MyPid());
        }
    }
}