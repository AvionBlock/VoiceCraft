using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core
{
    public class BackgroundProcess : IDisposable
    {
        private readonly Task _backgroundTask;
        private readonly CancellationTokenSource _cts;
        private bool _disposed;

        public BackgroundProcess(IBackgroundProcess process)
        {
            Process = process;
            _cts = new CancellationTokenSource();
            _backgroundTask = new Task(() => process.Start(_cts.Token));
        }

        public bool IsCompleted =>
            Status == BackgroundProcessStatus.Completed || Status == BackgroundProcessStatus.Error;

        public BackgroundProcessStatus Status => GetStatus();
        public IBackgroundProcess Process { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

            var sw = new SpinWait();
            while (!IsCompleted) sw.SpinOnce();
        }

        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(BackgroundProcess).ToString());
        }

        private BackgroundProcessStatus GetStatus()
        {
            switch (_backgroundTask.Status)
            {
                case TaskStatus.Created: return BackgroundProcessStatus.Stopped;
                case TaskStatus.Running: return BackgroundProcessStatus.Started;
                case TaskStatus.Faulted: return BackgroundProcessStatus.Error;
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                case TaskStatus.WaitingForActivation:
                case TaskStatus.WaitingForChildrenToComplete:
                case TaskStatus.WaitingToRun:
                default:
                    return BackgroundProcessStatus.Completed;
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (!_cts.IsCancellationRequested)
                    _cts.Cancel();
                _cts.Dispose();
                _backgroundTask.Dispose();
                Process.Dispose();
            }

            _disposed = true;
        }
    }
}