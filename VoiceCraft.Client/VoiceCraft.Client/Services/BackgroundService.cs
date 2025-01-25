using System;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class BackgroundService
    {
        protected event Action<IBackgroundProcess>? OnProcessStarted;
        
        protected event Action<IBackgroundProcess>? OnProcessStopped;

        public abstract void Test();

        public abstract void StartBackgroundProcess(IBackgroundProcess process);
        
        public abstract void StopBackgroundProcess<T>() where T : IBackgroundProcess;

        public abstract T GetBackgroundProcess<T>() where T : IBackgroundProcess;
        
        public abstract IBackgroundProcess[] GetAllBackgroundProcesses();
    }
}