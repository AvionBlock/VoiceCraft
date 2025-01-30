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
using VoiceCraft.Client.Android.Background;
using VoiceCraft.Client.Services;
using Exception = System.Exception;

namespace VoiceCraft.Client.Android
{
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
            try
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
                App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
                App.ServiceCollection.AddTransient<Permissions.Microphone>();

                Platform.Init(this, app);
                base.OnCreate(app);
                
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            }
            catch (Exception ex)
            {
                try
                {
                    CrashLogService.Log(ex); //Log it
                }
                catch (Exception writeEx)
                {
                    System.Diagnostics.Debug.WriteLine(writeEx); //We don't want to crash if the log failed.
                }
                throw; //rethrow so if logging fails, the system event handler should catch it.
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if(e.ExceptionObject is Exception ex)
                    CrashLogService.Log(ex); //Log it
            }
            catch (Exception writeEx)
            {
                System.Diagnostics.Debug.WriteLine(writeEx); //We don't want to crash if the log failed.
            }
        }
    }
}