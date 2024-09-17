using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using System.Diagnostics;

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
            Java.Lang.JavaSystem.LoadLibrary("openal");
            return base.CustomizeAppBuilder(builder)
                .WithInterFont();
        }
    }
}
