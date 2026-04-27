using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

[Activity(
    Label = "VoiceCraft",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? app)
    {
        base.OnCreate(app);
        Platform.Init(this, app);
    }
    
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
        Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    public override void OnBackPressed()
    {
        if (BackButtonBehavior()) return;
        #pragma warning disable
        base.OnBackPressed();
        #pragma warning restore
    }

    private static bool BackButtonBehavior()
    {
        if (App.ServiceProvider == null) return false;
        var navigationService = App.ServiceProvider.GetService<NavigationService>();
        return navigationService?.Back(true) != null;
    }
}
