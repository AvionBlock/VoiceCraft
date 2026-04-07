using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.iOS;

public class NativeBackgroundService(
    PermissionsService permissionsService,
    Func<Type, object> backgroundFactory)
    : IBackgroundService
{
    private static ConcurrentDictionary<Type, BackgroundTask> Services { get; } = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IosBackgroundExecutionKeeper _backgroundKeeper = new();
    private Func<Type, object> BackgroundFactory { get; } = backgroundFactory;

    public async Task StartServiceAsync<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull
    {
        await _semaphore.WaitAsync();
        try
        {
            if (await permissionsService.CheckAndRequestPermission<Permissions.Microphone>() !=
                PermissionStatus.Granted)
            {
                throw new PermissionException("BackgroundService.Permissions.MicrophoneNotGranted");
            }

            var backgroundType = typeof(T);
            if (Services.ContainsKey(backgroundType))
                throw new InvalidOperationException();

            if (BackgroundFactory.Invoke(backgroundType) is not T instance)
                throw new Exception($"Background task of type {backgroundType} is not of type {backgroundType}");

            BackgroundTask? backgroundTask = null;
            var keeperStarted = false;
            var serviceAdded = false;

            try
            {
                _backgroundKeeper.Start();
                keeperStarted = true;

                backgroundTask = new BackgroundTask(instance);
                backgroundTask.OnCompleted += BackgroundTaskOnCompleted;
                if (!Services.TryAdd(backgroundType, backgroundTask))
                    throw new InvalidOperationException();
                serviceAdded = true;

                backgroundTask.Start(() => startAction.Invoke(instance, _ => { }, _ => { }), () =>
                {
                    if (Services.IsEmpty)
                        _backgroundKeeper.Stop();
                });
            }
            catch
            {
                if (backgroundTask != null)
                {
                    backgroundTask.OnCompleted -= BackgroundTaskOnCompleted;
                    backgroundTask.Dispose();
                }

                if (serviceAdded)
                    Services.TryRemove(backgroundType, out _);

                if (keeperStarted && Services.IsEmpty)
                    _backgroundKeeper.Stop();

                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public T? GetService<T>() where T : notnull
    {
        if (Services.TryGetValue(typeof(T), out var service) && service.TaskInstance is T taskInstance)
            return taskInstance;
        return default;
    }

    public void Dispose()
    {
        foreach (var service in Services.Values)
            service.Dispose();

        Services.Clear();
        _backgroundKeeper.Dispose();
        GC.SuppressFinalize(this);
    }

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

        public void Start(Action startAction, Action onComplete)
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
                    onComplete.Invoke();
                }
            });

            var sw = new SpinWait();
            while (RunningTask.Status < TaskStatus.Running)
                sw.SpinOnce();
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
                // Do Nothing
            }

            GC.SuppressFinalize(this);
        }
    }
}
