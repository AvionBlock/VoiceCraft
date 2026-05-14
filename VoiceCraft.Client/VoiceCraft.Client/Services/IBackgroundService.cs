using System;
using System.Threading.Tasks;

namespace VoiceCraft.Client.Services
{
    public interface IBackgroundService : IDisposable
    {
        public Task StartServiceAsync<T>(Func<T, Action<string>, Action<string>, Task> startAction) where T : notnull;
        public T? GetService<T>() where T : notnull;
    }
}
