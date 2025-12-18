using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android.Background;

[Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
public class AndroidBackgroundService : Service
{
    private const int ErrorNotificationId = 999;
    private const int NotificationId = 1000;
    private const string ChannelId = "1001";

    internal static readonly ConcurrentDictionary<Type, BackgroundProcess> Processes = [];
    private static string _notificationTitle = string.Empty;
    private static string _notificationDescription = string.Empty;
    public static bool IsStarted { get; private set; }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (IsStarted) return StartCommandResult.Sticky; //Already running, return.
        IsStarted = true;

        var notification = CreateNotification();
        StartForeground(NotificationId, notification.Build());

        Task.Run(async () =>
        {
            try
            {
                while (!Processes.IsEmpty) //10 second wait time before self stopping activates (kinda).
                {
                    //Delay
                    await Task.Delay(500);
                    UpdateNotification();
                    foreach (var process in Processes)
                    {
                        if (process.Value.Status == BackgroundProcessStatus.Stopped)
                        {
                            process.Value.Process.OnUpdateTitle += ProcessOnUpdateTitle;
                            process.Value.Process.OnUpdateDescription += ProcessOnUpdateDescription;
                            process.Value.Start();
                            WeakReferenceMessenger.Default.Send(new ProcessStarted(process.Value.Process));
                            continue;
                        }

                        if (!process.Value.IsCompleted) continue;
                        if (!Processes.Remove(process.Key, out _)) continue;
                        process.Value.Process.OnUpdateTitle -= ProcessOnUpdateTitle;
                        process.Value.Process.OnUpdateDescription -= ProcessOnUpdateDescription;
                        process.Value.Dispose();
                        WeakReferenceMessenger.Default.Send(new ProcessStopped(process.Value.Process));
                    }
                }
            }
            catch (Exception ex)
            {
                var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                var errorNotification = CreateNotification();
                errorNotification.SetPriority((int)NotificationPriority.High);
                errorNotification.SetSmallIcon(ResourceConstant.Drawable.Icon);
                errorNotification.SetContentTitle("Background process error");
                //5000 characters so we don't annihilate the phone. Usually for debugging we only need the first 2000 characters
                errorNotification.SetStyle(new NotificationCompat.BigTextStyle().BigText(ex.ToString().Truncate(5000)));
                errorNotification.SetContentText(ex.GetType().ToString());
                notificationManager?.Notify(ErrorNotificationId, errorNotification.Build());
            }

            StopSelf();
        });

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        IsStarted = false;
        base.OnDestroy();
    }

    private static void ProcessOnUpdateTitle(string title)
    {
        _notificationTitle = title;
    }

    private static void ProcessOnUpdateDescription(string description)
    {
        _notificationDescription = description;
    }

    //Notification
    private static NotificationCompat.Builder CreateNotification()
    {
        var context = Application.Context;

        var notificationBuilder = new NotificationCompat.Builder(context, ChannelId);
        notificationBuilder.SetContentTitle("VoiceCraft");
        notificationBuilder.SetOngoing(true);

        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return notificationBuilder;
#pragma warning disable CA1416
        var notificationChannel = new NotificationChannel(ChannelId, "Background", NotificationImportance.Low);

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
        var notification = CreateNotification();
        notification.SetSmallIcon(ResourceConstant.Drawable.Icon);
        notification.SetContentTitle(string.IsNullOrWhiteSpace(_notificationTitle)
            ? "Running background processes"
            : Localizer.Get(_notificationTitle));
        notification.SetContentText(string.IsNullOrWhiteSpace(_notificationDescription)
            ? $"Background Processes: {Processes.Count}"
            : Localizer.Get(_notificationDescription));
        notificationManager?.Notify(NotificationId, notification.Build());
    }
}