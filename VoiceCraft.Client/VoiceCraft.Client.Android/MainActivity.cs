using System;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.Media.Audiofx;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Android.Audio;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

[Activity(
    Label = "VoiceCraft.Client.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
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
        App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>(x =>
        {
            var audioService = new NativeAudioService((AudioManager?)GetSystemService(AudioService) ??
                                                      throw new Exception(
                                                          $"Could not find {AudioService}. Cannot initialize audio service."));
            /*
            if (AutomaticGainControl.IsAvailable ||
                NoiseSuppressor.IsAvailable) //If one of these are available, we can add the native preprocessor
                audioService.RegisterPreprocessor<NativePreprocessor>(Guid.Empty, "Native Preprocessor");
            if (AcousticEchoCanceler.IsAvailable)
                audioService.RegisterEchoCanceler<NativeEchoCanceler>(Guid.Empty, "Native Echo Canceler");
                */
            return audioService;
        });
        
        Platform.Init(this, app);
        base.OnCreate(app);
    }
}