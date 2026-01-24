using System;
using System.Threading.Tasks;

namespace VoiceCraft.Client.Services
{
    public interface IBackgroundService : IDisposable
    {
        public Task<T> StartServiceAsync<T>(Action<T, Action<string>, Action<string>> startAction) where T : notnull;
        public T? GetService<T>() where T : notnull;
    }
}