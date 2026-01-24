using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android;

[Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
public class AndroidBackgroundService : Service
{
    private const int NotificationId = 1000;
    private const string ChannelId = "1001";
    public static string Title = "VoiceCraft";
    public static string Description = "Running...";
    public static bool IsStarted { get; private set; }

    public static ConcurrentDictionary<Type, NativeBackgroundService.BackgroundTask> Services { get; } = new();

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (IsStarted)
            return StartCommandResult.Sticky; //Already running, return.
        IsStarted = true;

        var notification = CreateNotification(Title, Description);
        StartForeground(NotificationId, notification.Build());
        Task.Run(BackgroundLogic);

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        IsStarted = false;
        foreach (var service in Services.Values)
        {
            service.Dispose();
        }
        base.OnDestroy();
    }

    private async Task BackgroundLogic()
    {
        try
        {
            while (!Services.IsEmpty)
            {
                //Delay
                await Task.Delay(500);
                UpdateNotification();
            }
        }
        catch
        {
            //Do Nothing
        }
        finally
        {
            StopSelf();
        }
    }

    //Notification
    private static NotificationCompat.Builder CreateNotification(string title, string description)
    {
        var context = Application.Context;

        var notificationBuilder = new NotificationCompat.Builder(context, ChannelId);
        notificationBuilder.SetContentTitle(Localizer.Get(title));
        notificationBuilder.SetContentText(Localizer.Get(description));
        notificationBuilder.SetOngoing(true);

        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return notificationBuilder;
#pragma warning disable CA1416
        var notificationChannel = new NotificationChannel(ChannelId, "Voice", NotificationImportance.Low);

        if (context.GetSystemService(NotificationService) is not NotificationManager notificationManager)
            return notificationBuilder;
        notificationBuilder.SetChannelId(ChannelId);
        notificationManager.CreateNotificationChannel(notificationChannel);
#pragma warning restore CA1416
        return notificationBuilder;
    }

    private void UpdateNotification()
    {
        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        var notification = CreateNotification(Title, Description);
        notification.SetSmallIcon(ResourceConstant.Drawable.Icon);
        notificationManager?.Notify(NotificationId, notification.Build());
    }
}