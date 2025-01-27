using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Background
{
    [Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
    public class AndroidBackgroundService : Service
    {
        public static bool IsStarted { get; private set; }

        private const int ErrorNotificationId = 999;
        private const int NotificationId = 1000;
        private const string ChannelId = "1001";
        private static readonly ConcurrentDictionary<IBackgroundProcess, Task> RunningBackgroundProcesses = [];
        private static string _notificationTitle = string.Empty;
        private static string _notificationDescription = string.Empty;

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
            WeakReferenceMessenger.Default.Register<GetBackgroundProcesses>(this, (_, m) =>
            {
                var backroundProcesses = RunningBackgroundProcesses.Select(x => x.Key);
                m.Reply(backroundProcesses);
            });

            Task.Run(async () =>
            {
                try
                {
                    var startTime = System.Environment.TickCount64;
                    while (!RunningBackgroundProcesses.IsEmpty ||
                           System.Environment.TickCount64 - startTime < 10000) //10 second wait time before self stopping activates (kinda).
                    {
                        RemoveCompletedProcesses();
                        QueueNextProcess();

                        //Update Notification.
                        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
                        notificationManager?.Notify(NotificationId,
                            CreateNotification()
                                .SetSmallIcon(ResourceConstant.Drawable.Icon)
                                .SetContentTitle(string.IsNullOrWhiteSpace(_notificationTitle) ? "Running background processes" : _notificationTitle)
                                .SetContentText(string.IsNullOrWhiteSpace(_notificationDescription)
                                    ? $"Background Processes: {RunningBackgroundProcesses.Count}"
                                    : _notificationDescription)
                                .Build());

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
                            .SetContentTitle("Background process error")
                            .SetStyle(new NotificationCompat.BigTextStyle().BigText(ex.ToString()
                                .Truncate(10000))) //10000 characters so we don't annihilate the phone. Usually for debugging we only need the first 2000 characters
                            .SetContentText(ex.GetType().ToString()).Build());
                    StopSelf();
                }
            });

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            IsStarted = false;
            base.OnDestroy();
        }

        private static void RemoveCompletedProcesses()
        {
            foreach (var backgroundProcess in RunningBackgroundProcesses)
            {
                if (!backgroundProcess.Value.IsCompleted) continue;
                backgroundProcess.Key.Dispose(); //Dispose Process
                backgroundProcess.Value.Dispose(); //Dispose Task
                backgroundProcess.Key.OnUpdateTitle -= ProcessOnUpdateTitle; //Deregister notification event.
                backgroundProcess.Key.OnUpdateDescription -= ProcessOnUpdateDescription; //Deregister notification event.
                RunningBackgroundProcesses.Remove(backgroundProcess.Key, out _); //Remove it.
                WeakReferenceMessenger.Default.Send(new ProcessStopped(backgroundProcess.Key));
            }
        }

        private static void QueueNextProcess()
        {
            var message = WeakReferenceMessenger.Default.Send<GetQueuedProcess>();
            if (!message.HasReceivedResponse || message.Response == null) return;
            message.Response.OnUpdateTitle += ProcessOnUpdateTitle;
            message.Response.OnUpdateDescription += ProcessOnUpdateDescription;
            RunningBackgroundProcesses.TryAdd(message.Response, Task.Run(() => message.Response.Start(), message.Response.TokenSource.Token));
            WeakReferenceMessenger.Default.Send(new ProcessStarted(message.Response));
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

        private static void ProcessOnUpdateTitle(string title)
        {
            _notificationTitle = title;
        }

        private static void ProcessOnUpdateDescription(string description)
        {
            _notificationDescription = description;
        }
    }

    public class GetQueuedProcess : RequestMessage<IBackgroundProcess?>;
}