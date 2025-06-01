using System;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.Media.Audiofx;
using Android.OS;
using AndroidX.Activity;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Android.Audio;
using VoiceCraft.Client.Android.Background;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
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
    private static readonly Guid EchoCancelerGuid = Guid.Parse("e6fdcab1-2a39-4b3c-a447-538648b9073b");
    private static readonly Guid NativeDenoiserGuid = Guid.Parse("2023A876-2824-4DC4-8700-5A98DA3EC5C7");
    private static readonly Guid NativeAutomaticGainControllerGuid = Guid.Parse("2EDE45FF-8D72-4A88-8657-1DEAD6EF9C50");

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
        CrashLogService.NativeStorageService = nativeStorage;
        CrashLogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(this));
        App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>(_ =>
        {
            var audioService = new NativeAudioService((AudioManager?)GetSystemService(AudioService) ?? throw new Exception(
                $"Could not find {AudioService}. Cannot initialize audio service."));

            //Register native preprocessors
            if (AcousticEchoCanceler.IsAvailable)
                audioService.RegisterEchoCanceler<NativeEchoCanceler>(EchoCancelerGuid, "Native Echo Canceler");
            if (NoiseSuppressor.IsAvailable)
                audioService.RegisterDenoiser<NativeDenoiser>(NativeDenoiserGuid, "Native Denoiser");
            if (AutomaticGainControl.IsAvailable)
                audioService.RegisterAutomaticGainController<NativeAutomaticGainController>(NativeAutomaticGainControllerGuid,
                    "Native Automatic Gain Controller");

            //Register Speex Preprocessors
            audioService.RegisterEchoCanceler<SpeexDspEchoCanceler>(Constants.SpeexDspEchoCancelerGuid, "SpeexDsp Echo Canceler");
            audioService.RegisterAutomaticGainController<SpeexDspAutomaticGainController>(Constants.SpeexDspAutomaticGainControllerGuid,
                "SpeexDsp Automatic Gain Controller");
            audioService.RegisterDenoiser<SpeexDspDenoiser>(Constants.SpeexDspDenoiserGuid, "SpeexDsp Denoiser");
            return audioService;
        });

        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
        App.ServiceCollection.AddTransient<Permissions.PostNotifications>();
        App.ServiceCollection.AddTransient<Permissions.Microphone>();

        Platform.Init(this, app);
        base.OnCreate(app);
    }

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