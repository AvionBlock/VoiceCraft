using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class BackgroundService
    {
        public abstract event Action<IBackgroundProcess>? OnProcessStarted;
        
        public abstract event Action<IBackgroundProcess>? OnProcessStopped;

        public abstract void StartBackgroundProcess(IBackgroundProcess process);

        public abstract T? GetBackgroundProcess<T>() where T : IBackgroundProcess;
        
        public abstract IEnumerable<IBackgroundProcess> GetBackgroundProcesses();
    }
}