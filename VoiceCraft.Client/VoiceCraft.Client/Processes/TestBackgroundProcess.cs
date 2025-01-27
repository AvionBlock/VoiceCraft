using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Processes;

public class TestBackgroundProcess : IBackgroundProcess
{
    public event Action<string>? OnUpdateTitle;
    public event Action<string>? OnUpdateDescription;
    public CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
    public void Start()
    {
        OnUpdateTitle?.Invoke("Test Title");
        OnUpdateDescription?.Invoke("Test Description");
        
        Task.Delay(20000, TokenSource.Token).Wait();
    }
    
    public void Dispose()
    {
        TokenSource.Cancel();
        TokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}