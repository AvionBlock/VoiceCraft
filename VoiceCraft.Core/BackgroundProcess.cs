using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class BackgroundProcess : IDisposable
    {
        public bool IsCompleted => Status == BackgroundProcessStatus.Completed || Status == BackgroundProcessStatus.Error;
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
            if (Status != BackgroundProcessStatus.Stopped) return;

            _backgroundTask.Start();
        }

        public void Stop()
        {
            ThrowIfDisposed();

            if (_cts.IsCancellationRequested) return;
            _cts.Cancel();
            
            while (!IsCompleted)
            {
                Thread.Sleep(10);
            }
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