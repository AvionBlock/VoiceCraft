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

namespace VoiceCraft.Client.Android
{
    [Activity(
        Label = "VoiceCraft.Client.Android",
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
            App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>(_ =>
            {
                var audioService = new NativeAudioService((AudioManager?)GetSystemService(AudioService) ?? throw new Exception(
                    $"Could not find {AudioService}. Cannot initialize audio service."));
                if (AcousticEchoCanceler.IsAvailable)
                    audioService.RegisterEchoCanceler<NativeEchoCanceler>(EchoCancelerGuid, "Native Echo Canceler");
                if (NoiseSuppressor.IsAvailable)
                    audioService.RegisterDenoiser<NativeDenoiser>(NativeDenoiserGuid, "Native Denoiser");
                if (AutomaticGainControl.IsAvailable)
                    audioService.RegisterAutomaticGainController<NativeAutomaticGainController>(NativeAutomaticGainControllerGuid,
                        "Native Automatic Gain Controller");
                return audioService;
            });
            App.ServiceCollection.AddTransient<Permissions.Microphone>();

            Platform.Init(this, app);
            base.OnCreate(app);
        }
    }
}