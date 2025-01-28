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
        private Task? _backgroundTask;
        private readonly ConcurrentQueue<IBackgroundProcess> _queuedProcesses = new();
        private readonly ConcurrentDictionary<IBackgroundProcess, Task> _runningBackgroundProcesses = [];
        public override event Action<IBackgroundProcess>? OnProcessStarted;
        public override event Action<IBackgroundProcess>? OnProcessStopped;
        
        private void StartService()
        {
            if (_backgroundTask != null) return;
            
            _backgroundTask = Task.Run(async () =>
            {
                try
                {
                    var startTime = Environment.TickCount64;
                    while (!_runningBackgroundProcesses.IsEmpty || 
                           Environment.TickCount64 - startTime < 10000) //10 second wait time before self stopping activates (kinda).
                    {
                        RemoveCompletedProcesses();
                        QueueNextProcess();
                        //Delay
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Invoke(() => notificationService.SendErrorNotification($"Background Error: {ex}"));
                }
                
                _backgroundTask = null;
            });
        }
        
        public override async Task<bool> StartBackgroundProcess(IBackgroundProcess process, int timeout = 5000)
        {
            StartService();
            _queuedProcesses.Enqueue(process);
            var startTime = Environment.TickCount64;
            while (_queuedProcesses.Contains(process))
            {
                if (Environment.TickCount64 - startTime >= timeout)
                    return false;
                
                await Task.Delay(50);
            }

            return true;
        }

        public override T? GetBackgroundProcess<T>() where T : default
        {
            var processes = GetBackgroundProcesses();
            foreach (var process in processes)
            {
                if(process.GetType() == typeof(T))
                    return (T)process;
            }
            return default;
        }

        public override IEnumerable<IBackgroundProcess> GetBackgroundProcesses()
        {
            return _runningBackgroundProcesses.Select(x => x.Key);
        }
        
        private void RemoveCompletedProcesses()
        {
            foreach (var backgroundProcess in _runningBackgroundProcesses)
            {
                if (!backgroundProcess.Value.IsCompleted) continue;
                backgroundProcess.Key.Dispose(); //Dispose Process
                backgroundProcess.Value.Dispose(); //Dispose Task
                _runningBackgroundProcesses.Remove(backgroundProcess.Key, out _); //Remove it.
                OnProcessStopped?.Invoke(backgroundProcess.Key);
            }
        }

        private void QueueNextProcess()
        {
            if (!_queuedProcesses.TryDequeue(out var process)) return;
            _runningBackgroundProcesses.TryAdd(process, Task.Run(() => process.Start(), process.TokenSource.Token));
            OnProcessStarted?.Invoke(process);
        }
    }
}