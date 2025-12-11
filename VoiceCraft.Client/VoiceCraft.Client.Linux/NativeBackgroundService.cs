using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Linux;

public class NativeBackgroundService(NotificationService notificationService) : BackgroundService
{
    private readonly ConcurrentDictionary<Type, BackgroundProcess> _processes = [];
    private Task? _backgroundWorker;
    public override event Action<IBackgroundProcess>? OnProcessStarted;
    public override event Action<IBackgroundProcess>? OnProcessStopped;

    public override async Task StartBackgroundProcess<T>(T process, int timeout = 5000)
    {
        var processType = typeof(T);
        if (_processes.ContainsKey(processType))
            //TODO Locale This!
            throw new InvalidOperationException("A background process of this type has already been queued/started!");

        var backgroundProcess = new BackgroundProcess(process);
        _processes.TryAdd(processType, backgroundProcess);
        if (!StartBackgroundWorker())
        {
            _processes.Clear();
            //TODO Locale This!
            throw new Exception("Failed to start background process! Background worker failed to start!");
        }

        var startTime = DateTime.UtcNow;
        while (backgroundProcess.Status == BackgroundProcessStatus.Stopped)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds >= timeout)
            {
                _processes.TryRemove(processType, out _);
                backgroundProcess.Dispose();
                //TODO Locale This!
                throw new Exception("Failed to start background process!");
            }

            await Task.Delay(10); //Don't burn the CPU!
        }
    }

    public override Task StopBackgroundProcess<T>()
    {
        var processType = typeof(T);
        if (!_processes.TryRemove(processType, out var process)) return Task.CompletedTask;
        process.Stop();
        process.Dispose();
        OnProcessStopped?.Invoke(process.Process);
        return Task.CompletedTask;
    }

    public override bool TryGetBackgroundProcess<T>(out T? process) where T : default
    {
        var processType = typeof(T);
        if (!_processes.TryGetValue(processType, out var value))
        {
            process = default;
            return false;
        }

        process = (T?)value.Process;
        return process != null;
    }

    private bool StartBackgroundWorker()
    {
        if (_backgroundWorker is { IsCompleted: false }) return true;
        _backgroundWorker = Task.Run(BackgroundLogic);
        return true;
    }

    private async Task BackgroundLogic()
    {
        try
        {
            while (!_processes.IsEmpty)
            {
                await Task.Delay(500);
                foreach (var process in _processes)
                {
                    if (process.Value.Status == BackgroundProcessStatus.Stopped)
                    {
                        process.Value.Start();
                        OnProcessStarted?.Invoke(process.Value.Process);
                        continue;
                    }

                    if (!process.Value.IsCompleted) continue;
                    if (!_processes.Remove(process.Key, out _)) continue;
                    process.Value.Dispose();
                    OnProcessStopped?.Invoke(process.Value.Process);
                }
            }
        }
        catch (Exception ex)
        {
            //TODO Locale This!
            notificationService.SendErrorNotification($"Background Error: {ex}");
        }
    }
}