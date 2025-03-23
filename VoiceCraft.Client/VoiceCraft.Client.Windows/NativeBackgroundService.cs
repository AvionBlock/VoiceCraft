using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Windows
{
    public class NativeBackgroundService(NotificationService notificationService) : BackgroundService
    {
        private Task? _backgroundWorker;

        private readonly ConcurrentQueue<KeyValuePair<Type, IBackgroundProcess>> _queuedProcesses = new();
        private readonly ConcurrentDictionary<Type, KeyValuePair<Task, IBackgroundProcess>> _runningBackgroundProcesses = [];
        public override event Action<IBackgroundProcess>? OnProcessStarted;
        public override event Action<IBackgroundProcess>? OnProcessStopped;

        protected override bool StartBackgroundWorker()
        {
            if (_backgroundWorker is { IsCompleted: false }) return true;
            _backgroundWorker = Task.Run(async () =>
            {
                try
                {
                    while (!_queuedProcesses.IsEmpty || !_runningBackgroundProcesses.IsEmpty)
                    {
                        await Task.Delay(500);
                        ClearCompletedProcesses();
                        
                        if (!_queuedProcesses.TryDequeue(out var process)) continue;
                        var task = Task.Run(() => process.Value.Start(), process.Value.TokenSource.Token);
                        _runningBackgroundProcesses.TryAdd(process.Key, new KeyValuePair<Task, IBackgroundProcess>(task, process.Value));
                        OnProcessStarted?.Invoke(process.Value);
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Invoke(() => notificationService.SendErrorNotification($"Background Error: {ex}"));
                }
            });
            return true;
        }

        public override async Task StartBackgroundProcess<T>(T process, int timeout = 5000)
        {
            var processType = typeof(T);
            if (_queuedProcesses.Any(x => x.Key == processType) || _runningBackgroundProcesses.ContainsKey(processType))
                throw new InvalidOperationException("A background process of this type has already been queued/started!");
            
            _queuedProcesses.Enqueue(new KeyValuePair<Type, IBackgroundProcess>(processType, process));
            if (!StartBackgroundWorker())
            {
                _queuedProcesses.Clear();
                throw new Exception("Failed to start background process! Background worker failed to start!");
            }

            var startTime = Environment.TickCount64;
            while (!_runningBackgroundProcesses.ContainsKey(processType))
            {
                if (Environment.TickCount64 - startTime >= timeout)
                    throw new Exception("Failed to start background process!");
                await Task.Delay(10); //Don't burn the CPU!
            }
        }

        public override async Task StopBackgroundProcess<T>()
        {
            var processType = typeof(T);
            if (_runningBackgroundProcesses.TryRemove(processType, out var process))
            {
                if(!process.Value.TokenSource.IsCancellationRequested)
                    await process.Value.TokenSource.CancelAsync();
                while (!process.Key.IsCompleted)
                {
                    await Task.Delay(10); //Don't burn the CPU!
                }
                process.Value.Dispose();
                process.Key.Dispose();
                OnProcessStopped?.Invoke(process.Value);
            }
        }

        public override bool TryGetBackgroundProcess<T>(out T? process) where T : default
        {
            var processType = typeof(T);
            if (!_runningBackgroundProcesses.TryGetValue(processType, out var value))
            {
                process = default;
                return false;
            }
            
            process = (T?)value.Value;
            return process != null;
        }

        private void ClearCompletedProcesses()
        {
            foreach (var process in _runningBackgroundProcesses)
            {
                if (!process.Value.Key.IsCompleted || !_runningBackgroundProcesses.Remove(process.Key, out _)) continue;
                process.Value.Value.Dispose();
                process.Value.Key.Dispose();
                OnProcessStopped?.Invoke(process.Value.Value);
            }
        }
    }
}