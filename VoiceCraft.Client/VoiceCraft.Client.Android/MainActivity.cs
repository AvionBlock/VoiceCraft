using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using System.IO;

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
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
#if DEBUG
            if (!Directory.Exists(App.PluginDirectory))
            {
                Directory.CreateDirectory(App.PluginDirectory);
            }

            //Always copy DLL over so we don't need to manually uninstall the app.
            using (var fileAssetStream = Assets.Open("VoiceCraft.Client.Plugin.dll"))
            using (var fileStream = File.Create($"{App.PluginDirectory}/VoiceCraft.Client.Plugin.dll"))
            {
                fileAssetStream.CopyTo(fileStream);
            }
#elif RELEASE
            if (!Directory.Exists(App.PluginDirectory))
            {
                Directory.CreateDirectory(App.PluginDirectory);
                using(var fileAssetStream = Assets.Open("VoiceCraft.Client.Plugin.dll"))
                using (var fileStream = File.Create($"{App.PluginDirectory}/VoiceCraft.Client.Plugin.dll"))
                {
                    fileAssetStream.CopyTo(fileStream);
                }
            }
#endif
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
