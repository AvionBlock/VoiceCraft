using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Windows;

public class NativeBackgroundService(Func<Type, object> backgroundFactory) : IBackgroundService
{
    private static ConcurrentDictionary<Type, BackgroundTask> Services { get; } = new();
    private Func<Type, object> BackgroundFactory { get; } = backgroundFactory;

    public Task<T> StartServiceAsync<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull
    {
        var backgroundType = typeof(T);
        if (Services.ContainsKey(backgroundType))
            throw new InvalidOperationException();

        if (BackgroundFactory.Invoke(backgroundType) is not T instance)
            throw new Exception($"Background task of type {backgroundType} is not of type {backgroundType}");

        var backgroundTask = new BackgroundTask(instance);
        backgroundTask.OnCompleted += BackgroundTaskOnCompleted;
        Services.TryAdd(backgroundType, backgroundTask);
        backgroundTask.Start(() => startAction.Invoke(instance, _ => {}, _ => {}));
        return Task.FromResult(instance);
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
        {
            service.Dispose();
        }
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