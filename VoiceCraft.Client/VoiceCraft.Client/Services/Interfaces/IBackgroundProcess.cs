using System;
using System.Threading;

namespace VoiceCraft.Client.Services.Interfaces
{
    public interface IBackgroundProcess : IDisposable
    {
        BackgroundProcessStatus Status { get; set; }
        event Action<string>? OnUpdateTitle;
        event Action<string>? OnUpdateDescription;
        CancellationTokenSource TokenSource { get; }
        void Start();
    }

    public enum BackgroundProcessStatus
    {
        Stopped,
        Starting,
        Started,
        Completed,
        Error
    }
}