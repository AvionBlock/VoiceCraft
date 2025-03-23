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

        internal static readonly ConcurrentQueue<KeyValuePair<Type, IBackgroundProcess>> QueuedProcesses = [];
        internal static readonly ConcurrentDictionary<Type, KeyValuePair<Task, IBackgroundProcess>> RunningBackgroundProcesses = [];
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

            WeakReferenceMessenger.Default.Register<StopBackgroundProcess>(this, (_, m) =>
            {
                if (RunningBackgroundProcesses.TryRemove(m.ProcessType, out var process))
                {
                    if (!process.Value.TokenSource.IsCancellationRequested)
                        process.Value.TokenSource.Cancel();
                    while (!process.Key.IsCompleted)
                    {
                        Task.Delay(10).GetAwaiter().GetResult();
                    }

                    process.Value.Dispose();
                    process.Key.Dispose();
                    m.Reply(process.Value);
                }
                m.Reply(null);
            });

            Task.Run(async () =>
            {
                try
                {
                    while (!QueuedProcesses.IsEmpty && !RunningBackgroundProcesses.IsEmpty) //10 second wait time before self stopping activates (kinda).
                    {
                        //Delay
                        await Task.Delay(500);
                        ClearCompletedProcesses();
                        UpdateNotification();
                        if (!QueuedProcesses.TryDequeue(out var process)) continue;

                        process.Value.OnUpdateTitle += ProcessOnUpdateTitle;
                        process.Value.OnUpdateDescription += ProcessOnUpdateDescription;
                        
                        var task = Task.Run(() => process.Value.Start(), process.Value.TokenSource.Token);
                        RunningBackgroundProcesses.TryAdd(process.Key, new KeyValuePair<Task, IBackgroundProcess>(task, process.Value));
                        WeakReferenceMessenger.Default.Send(new ProcessStarted(process.Value));
                    }
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
                                .Truncate(5000))) //5000 characters so we don't annihilate the phone. Usually for debugging we only need the first 2000 characters
                            .SetContentText(ex.GetType().ToString()).Build());
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
        
        private static void ClearCompletedProcesses()
        {
            foreach (var process in RunningBackgroundProcesses)
            {
                process.Value.Value.Dispose();
                process.Value.Key.Dispose();
                if (!process.Value.Key.IsCompleted || !RunningBackgroundProcesses.Remove(process.Key, out _)) continue;
                process.Value.Value.OnUpdateTitle -= ProcessOnUpdateTitle;
                process.Value.Value.OnUpdateDescription -= ProcessOnUpdateDescription;
                WeakReferenceMessenger.Default.Send(new ProcessStopped(process.Value.Value));
            }
        }

        //Notification
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

        private void UpdateNotification()
        {
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.Notify(NotificationId,
                CreateNotification()
                    .SetSmallIcon(ResourceConstant.Drawable.Icon)
                    .SetContentTitle(string.IsNullOrWhiteSpace(_notificationTitle) ? "Running background processes" : _notificationTitle)
                    .SetContentText(string.IsNullOrWhiteSpace(_notificationDescription)
                        ? $"Background Processes: {RunningBackgroundProcesses.Count}"
                        : _notificationDescription)
                    .Build());
        }
    }

    public class GetQueuedProcess : RequestMessage<IBackgroundProcess?>;
}