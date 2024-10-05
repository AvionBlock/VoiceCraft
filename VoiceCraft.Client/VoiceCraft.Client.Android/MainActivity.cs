using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VoiceCraft.Client.Android.Audio;
using VoiceCraft.Client.PDK.Audio;

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
        //Shutup
        public static AudioHelper AudioHelper = default!;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            AudioHelper = new AudioHelper(this);

            AudioHelper.PlayWithSpeaker();
#if DEBUG
            if (!Directory.Exists(App.PluginDirectory))
            {
                Directory.CreateDirectory(App.PluginDirectory);
            }

            //Always copy DLL over so we don't need to manually uninstall the app.
            using (var fileAssetStream = Assets?.Open("VoiceCraft.Client.Plugin.dll"))
            {
                if (fileAssetStream != null)
                {
                    using (var fileStream = File.Create($"{App.PluginDirectory}/VoiceCraft.Client.Plugin.dll"))
                    {
                        fileAssetStream.CopyTo(fileStream);
                    }
                }
            }
#elif RELEASE
            if (!Directory.Exists(App.PluginDirectory))
            {
                Directory.CreateDirectory(App.PluginDirectory);
                using (var fileAssetStream = Assets?.Open("VoiceCraft.Client.Plugin.dll"))
                {
                    if (fileAssetStream != null)
                    {
                        using (var fileStream = File.Create($"{App.PluginDirectory}/VoiceCraft.Client.Plugin.dll"))
                        {
                            fileAssetStream.CopyTo(fileStream);
                        }
                    }
                }
            }
#endif

            App.Services.AddSingleton<IAudioPlayer, AudioPlayer>();
            App.Services.AddSingleton<IAudioRecorder, AudioRecorder>();
            App.Services.AddSingleton<IAudioDevices, AudioDevices>();

            base.OnCreate(savedInstanceState);
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
