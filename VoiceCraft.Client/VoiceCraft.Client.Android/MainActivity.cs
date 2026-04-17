using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;
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
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
        Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    protected override void OnCreate(Bundle? app)
    {
        OnBackPressedDispatcher.AddCallback(this, new BackPressedCallback(this));

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
