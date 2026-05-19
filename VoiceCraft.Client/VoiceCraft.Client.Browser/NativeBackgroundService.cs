using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser;

public class NativeBackgroundService(Func<Type, object> backgroundFactory) : IBackgroundService
{
    private static readonly ConcurrentDictionary<Type, BackgroundTask> Services = new();

    public async Task StartServiceAsync<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull
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
            await backgroundTask.StartAsync(() => startAction.Invoke(instance, _ => { }, _ => { }));
        }
        catch
        {
            Services.TryRemove(backgroundType, out _);
            backgroundTask.OnCompleted -= BackgroundTaskOnCompleted;
            backgroundTask.Dispose();
            throw;
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

        public async Task StartAsync(Action startAction)
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
            
            while (RunningTask.Status < TaskStatus.Running)
            {
                await Task.Delay(10);
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
                // Do nothing.
            }

            GC.SuppressFinalize(this);
        }
    }
}
