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
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Background
{
    [Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
    public class AndroidBackgroundService : Service
    {
        private const int ErrorNotificationId = 999;
        private const int NotificationId = 1000;
        private const string ChannelId = "1001";
        private static bool _isStarted;
        private static readonly ConcurrentQueue<IBackgroundProcess> BackgroundProcesses = new();
        private static readonly List<Task> RunningBackgroundProcesses = [];

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (_isStarted) return StartCommandResult.Sticky; //Already running, return.
            _isStarted = true;

            var notification = CreateNotification();
            StartForeground(NotificationId, notification.Build());

            WeakReferenceMessenger.Default.Register<StartBackgroundProcess>(this, (r, m) => { BackgroundProcesses.Enqueue(m.Value); });

            Task.Run(async () =>
            {
                try
                {
                    var startTime = System.Environment.TickCount64;
                    while (!BackgroundProcesses.IsEmpty || !BackgroundProcesses.IsEmpty ||
                           System.Environment.TickCount64 - startTime < 10000) //10 second wait time before self stopping activates (kinda).
                    {
                        RunningBackgroundProcesses.RemoveAll(x => x.IsCompleted); //Remove completed processes

                        if (BackgroundProcesses.TryDequeue(out var process)) //Dequeue next process and start it.
                        {
                            RunningBackgroundProcesses.Add(Task.Run(() => process.Start()));
                        }

                        //Update Notification.
                        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                        notificationManager?.Notify(NotificationId,
                            CreateNotification()
                                .SetSmallIcon(ResourceConstant.Drawable.Icon).SetContentTitle("Running Background Processes")
                                .SetContentText($"Background Processes: {RunningBackgroundProcesses.Count}").Build());

                        //Delay
                        await Task.Delay(500);
                    }

                    StopSelf();
                }
                catch (Exception ex)
                {
                    var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                    notificationManager?.Notify(ErrorNotificationId,
                        CreateNotification()
                            .SetPriority((int)NotificationPriority.High)
                            .SetSmallIcon(ResourceConstant.Drawable.Icon)
                            .SetContentTitle("Background Process Error")
                            .SetStyle(new NotificationCompat.BigTextStyle().BigText(ex.ToString()
                                .Truncate(10000))) //10000 characters so we don't annihilate the phone.
                            .SetContentText(ex.GetType().ToString()).Build());
                    StopSelf();
                }
            });

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _isStarted = false;
            base.OnDestroy();
        }

        private static NotificationCompat.Builder CreateNotification()
        {
            var context = Application.Context;

            var notificationBuilder = new NotificationCompat.Builder(context, ChannelId)
                .SetContentTitle("VoiceCraft")
                .SetOngoing(true);

            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return notificationBuilder;
#pragma warning disable CA1416
            var notificationChannel = new NotificationChannel(ChannelId, "Background", NotificationImportance.Low);

            if (context.GetSystemService(NotificationService) is not NotificationManager notificationManager) return notificationBuilder;
            notificationBuilder.SetChannelId(ChannelId);
            notificationManager.CreateNotificationChannel(notificationChannel);
#pragma warning restore CA1416
            return notificationBuilder;
        }
    }
}