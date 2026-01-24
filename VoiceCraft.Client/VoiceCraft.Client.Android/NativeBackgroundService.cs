using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android;

[Service(ForegroundServiceType = ForegroundService.TypeMicrophone)]
public class NativeBackgroundService(PermissionsService permissionsService, Func<Type, object> backgroundFactory)
    : Service, IBackgroundService
{
    private const int NotificationId = 1000;
    private const string ChannelId = "1001";
    private string _title = "VoiceCraft";
    private string _description = "Running...";
    private static bool _isStarted;

    private static ConcurrentDictionary<Type, BackgroundTask> Services { get; } = new();
    private Func<Type, object> BackgroundFactory { get; } = backgroundFactory;

    public T StartService<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull
    {
        StartBackgroundService().GetAwaiter().GetResult();
        var backgroundType = typeof(T);
        if (Services.ContainsKey(backgroundType))
            throw new InvalidOperationException();

        if (BackgroundFactory.Invoke(backgroundType) is not T instance)
            throw new Exception($"Background task of type {backgroundType} is not of type {backgroundType}");

        var backgroundTask = new BackgroundTask(instance);
        backgroundTask.OnCompleted += BackgroundTaskOnCompleted;
        Services.TryAdd(backgroundType, backgroundTask);
        backgroundTask.Start(() => startAction.Invoke(instance, UpdateTitle, UpdateDescription));
        return instance;
    }

    public T? GetService<T>() where T : notnull
    {
        if (Services.TryGetValue(typeof(T), out var service) && service.TaskInstance is T taskInstance)
            return taskInstance;
        return default;
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_isStarted) return StartCommandResult.Sticky; //Already running, return.
        _isStarted = true;

        var notification = CreateNotification(_title, _description);
        StartForeground(NotificationId, notification.Build());
        Task.Run(BackgroundLogic);

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _isStarted = false;
        foreach (var service in Services.Values)
        {
            service.Dispose();
        }
        base.OnDestroy();
    }

    private async Task StartBackgroundService()
    {
        if (_isStarted) return;
        //Don't care if it's granted or not.
        await permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
            "Notifications are required to show running background processes and errors.");

        var context = Application.Context;
        var intent = new Intent(context, typeof(NativeBackgroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            //Shut the fuck up.
#pragma warning disable CA1416
            context.StartForegroundService(intent);
#pragma warning restore CA1416
        else
            context.StartService(intent);
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
    
    private void UpdateTitle(string title)
    {
        _title = title;
    }

    private void UpdateDescription(string description)
    {
        _description = description;
    }

    private void UpdateNotification()
    {
        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        var notification = CreateNotification(_title, _description);
        notification.SetSmallIcon(ResourceConstant.Drawable.Icon);
        notificationManager?.Notify(NotificationId, notification.Build());
    }
    
    //Events
    private static void BackgroundTaskOnCompleted(BackgroundTask task)
    {
        task.OnCompleted -= BackgroundTaskOnCompleted;
        Services.TryRemove(task.TaskInstance.GetType(), out _);
    }

    private class BackgroundTask(object taskInstance) : IDisposable
    {
        public event Action<BackgroundTask>? OnCompleted;
        public Task? RunningTask { get; private set; }
        public object TaskInstance { get; } = taskInstance;

        public void Start(Action startAction)
        {
            RunningTask = Task.Run(() =>
            {
                try
                {
                    startAction.Invoke();
                }
                finally
                {
                    Dispose();
                    OnCompleted?.Invoke(this);
                }
            });

            var sw = new SpinWait();
            while (RunningTask.Status < TaskStatus.Running)
            {
                sw.SpinOnce();
            }
        }

        public void Dispose()
        {
            try
            {
                if (TaskInstance is IDisposable disposable)
                    disposable.Dispose();
            }
            catch
            {
                //Do Nothing
            }

            GC.SuppressFinalize(this);
        }
    }
}