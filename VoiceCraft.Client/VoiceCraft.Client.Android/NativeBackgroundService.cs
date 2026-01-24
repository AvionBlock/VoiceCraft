using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

public class NativeBackgroundService(PermissionsService permissionsService, Func<Type, object> backgroundFactory) : IBackgroundService
{
    private Func<Type, object> BackgroundFactory { get; } = backgroundFactory;

    public async Task StartServiceAsync<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull
    {
        var backgroundType = typeof(T);
        if (AndroidBackgroundService.Services.ContainsKey(backgroundType))
            throw new InvalidOperationException();

        if (BackgroundFactory.Invoke(backgroundType) is not T instance)
            throw new Exception($"Background task of type {backgroundType} is not of type {backgroundType}");

        var backgroundTask = new BackgroundTask(instance);
        backgroundTask.OnCompleted += BackgroundTaskOnCompleted;
        AndroidBackgroundService.Services.TryAdd(backgroundType, backgroundTask);
        await StartBackgroundService();
        backgroundTask.Start(() => startAction.Invoke(instance, UpdateTitle, UpdateDescription));
    }

    public T? GetService<T>() where T : notnull
    {
        if (AndroidBackgroundService.Services.TryGetValue(typeof(T), out var service) && service.TaskInstance is T taskInstance)
            return taskInstance;
        return default;
    }
    
    public void Dispose()
    {
        var context = Application.Context;
        var intent = new Intent(context, typeof(NativeBackgroundService));
        context.StopService(intent);
        GC.SuppressFinalize(this);
    }
    
    private static void UpdateTitle(string title)
    {
        AndroidBackgroundService.Title = title;
    }

    private static void UpdateDescription(string description)
    {
        AndroidBackgroundService.Description = description;
    }

    private async Task StartBackgroundService()
    {
        if (AndroidBackgroundService.IsStarted) return;
        //Don't care if it's granted or not.
        await permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
            "Notifications are required to show running background processes and errors.");
        
        if (await permissionsService.CheckAndRequestPermission<Permissions.Microphone>() !=
            PermissionStatus.Granted) { 
            throw new PermissionException("Microphone access not granted!");
        }

        var context = Application.Context;
        var intent = new Intent(context, typeof(AndroidBackgroundService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            //Shut the fuck up.
#pragma warning disable CA1416
            context.StartForegroundService(intent);
#pragma warning restore CA1416
        else
            context.StartService(intent);
    }
    
    //Events
    private static void BackgroundTaskOnCompleted(BackgroundTask task)
    {
        task.OnCompleted -= BackgroundTaskOnCompleted;
        AndroidBackgroundService.Services.TryRemove(task.TaskInstance.GetType(), out _);
    }

    public class BackgroundTask(object taskInstance) : IDisposable
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