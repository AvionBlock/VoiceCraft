using System;
using System.Threading;

namespace VoiceCraft.Core.Interfaces
{
    public interface IBackgroundProcess : IDisposable
    {
        event Action<string>? OnUpdateTitle;
        event Action<string>? OnUpdateDescription;
        void Start(CancellationToken cancellationToken);
    }
}