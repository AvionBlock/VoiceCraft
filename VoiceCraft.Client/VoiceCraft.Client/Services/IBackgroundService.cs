using System;

namespace VoiceCraft.Client.Services
{
    public interface IBackgroundService : IDisposable
    {
        public T StartService<T>(Action<T> startAction) where T : notnull;
        public T? GetService<T>() where T : notnull;
    }
}