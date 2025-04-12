using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Windows
{
        public class NativeBackgroundService(NotificationService notificationService) : BackgroundService
    {
        private Task? _backgroundWorker;
        
        private readonly ConcurrentDictionary<Type, BackgroundProcess> _processes = [];
        public override event Action<IBackgroundProcess>? OnProcessStarted;
        public override event Action<IBackgroundProcess>? OnProcessStopped;

        public override async Task StartBackgroundProcess<T>(T process, int timeout = 5000)
        {
            var processType = typeof(T);
            if (_processes.ContainsKey(processType))
                throw new InvalidOperationException("A background process of this type has already been queued/started!");

            var backgroundProcess = new BackgroundProcess(process);
            _processes.TryAdd(processType, backgroundProcess);
            if (!StartBackgroundWorker())
            {
                _processes.Clear();
                throw new Exception("Failed to start background process! Background worker failed to start!");
            }

            var startTime = DateTime.UtcNow;
            while (backgroundProcess.Status == BackgroundProcessStatus.Stopped)
            {
                if ((DateTime.UtcNow - startTime).TotalMilliseconds >= timeout)
                {
                    _processes.TryRemove(processType, out _);
                    backgroundProcess.Dispose();
                    throw new Exception("Failed to start background process!");
                }

                await Task.Delay(10); //Don't burn the CPU!
            }
        }

        public override async Task StopBackgroundProcess<T>()
        {
            var processType = typeof(T);
            if (_processes.TryRemove(processType, out var process))
            {
                await process.StopAsync();
                while (!process.IsCompleted)
                {
                    await Task.Delay(10); //Don't burn the CPU!
                }
                process.Dispose();
                OnProcessStopped?.Invoke(process.Process);
            }
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

                        if (process.Value.IsCompleted && _processes.Remove(process.Key, out _)) continue;
                        process.Value.Dispose();
                        OnProcessStopped?.Invoke(process.Value.Process);
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() => notificationService.SendErrorNotification($"Background Error: {ex}"));
            }
        }

        private class BackgroundProcess : IDisposable
        {
            public bool IsCompleted => Status is BackgroundProcessStatus.Completed or BackgroundProcessStatus.Error;
            public BackgroundProcessStatus Status => GetStatus();
            public IBackgroundProcess Process { get; }
            
            private readonly Task _backgroundTask;
            private readonly CancellationTokenSource _cts;
            private bool _disposed;

            public BackgroundProcess(IBackgroundProcess process)
            {
                Process = process;
                _cts = new CancellationTokenSource();
                _backgroundTask = new Task(() => process.Start(_cts.Token), _cts.Token);
            }

            public void Start()
            {
                ThrowIfDisposed();
                if(Status != BackgroundProcessStatus.Stopped) return;
                
                _backgroundTask.Start();
            }

            public async Task StopAsync()
            {
                ThrowIfDisposed();
                
                if(_cts.IsCancellationRequested) return;
                await _cts.CancelAsync();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void ThrowIfDisposed()
            {
                if (!_disposed) return;
                throw new ObjectDisposedException(typeof(BackgroundProcess).ToString());
            }
            
            private BackgroundProcessStatus GetStatus()
            {
                if (_backgroundTask.IsFaulted)
                    return BackgroundProcessStatus.Error;
                return _backgroundTask.IsCompleted ? BackgroundProcessStatus.Completed : BackgroundProcessStatus.Started;
            }

            private void Dispose(bool disposing)
            {
                if (_disposed) return;

                if (disposing)
                {
                    _cts.Dispose();
                    _backgroundTask.Dispose();
                    Process.Dispose();
                }
                
                _disposed = true;
            }
        }
    }
}