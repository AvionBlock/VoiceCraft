using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser;

public sealed class NativeBackgroundService(Func<Type, object> backgroundFactory) : IBackgroundService
{
    private static readonly ConcurrentDictionary<Type, BackgroundTask> Services = new();

    public Task StartServiceAsync<T>(Func<T, Action<string>, Action<string>, Task> startAction) where T : notnull
    {
        var backgroundType = typeof(T);
        if (Services.ContainsKey(backgroundType))
            throw new InvalidOperationException();

        if (backgroundFactory.Invoke(backgroundType) is not T instance)
            throw new Exception($"Background task of type {backgroundType} is not of type {backgroundType}");

        var backgroundTask = new BackgroundTask(instance);
        backgroundTask.OnCompleted += BackgroundTaskOnCompleted;
        try
        {
            Services.TryAdd(backgroundType, backgroundTask);
            backgroundTask.Start(() => startAction.Invoke(instance, _ => { }, _ => { }));
        }
        catch
        {
            Services.TryRemove(backgroundType, out _);
            backgroundTask.OnCompleted -= BackgroundTaskOnCompleted;
            backgroundTask.Dispose();
            throw;
        }

        return Task.CompletedTask;
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

        GC.SuppressFinalize(this);
    }

    private static void BackgroundTaskOnCompleted(BackgroundTask task)
    {
        task.OnCompleted -= BackgroundTaskOnCompleted;
        Services.TryRemove(task.TaskInstance.GetType(), out _);
    }

    private sealed class BackgroundTask(object taskInstance) : IDisposable
    {
        public event Action<BackgroundTask>? OnCompleted;
        public Task? RunningTask { get; private set; }
        public object TaskInstance { get; } = taskInstance;

        public void Start(Func<Task> startAction)
        {
            RunningTask = Task.Run(async () =>
            {
                try
                {
                    await startAction.Invoke();
                }
                finally
                {
                    Dispose();
                    OnCompleted?.Invoke(this);
                }
            });
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
                // Do nothing.
            }

            GC.SuppressFinalize(this);
        }
    }
}
