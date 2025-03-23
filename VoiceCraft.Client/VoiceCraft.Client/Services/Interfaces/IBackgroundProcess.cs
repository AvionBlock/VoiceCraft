using System;
using System.Threading;

namespace VoiceCraft.Client.Services.Interfaces
{
    public interface IBackgroundProcess : IDisposable
    {
        bool IsStarted { get; }
        event Action<string>? OnUpdateTitle;
        event Action<string>? OnUpdateDescription;
        CancellationTokenSource TokenSource { get; }
        void Start();
    }
}