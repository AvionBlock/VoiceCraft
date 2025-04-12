using System;
using System.Threading.Tasks;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class BackgroundService
    {
        public abstract event Action<IBackgroundProcess>? OnProcessStarted;
        
        public abstract event Action<IBackgroundProcess>? OnProcessStopped;

        public abstract Task StartBackgroundProcess<T>(T process, int timeout = 5000) where T : IBackgroundProcess;
        
        public abstract Task StopBackgroundProcess<T>() where T : IBackgroundProcess;
        
        public abstract bool TryGetBackgroundProcess<T>(out T? process) where T : IBackgroundProcess;
    }
}