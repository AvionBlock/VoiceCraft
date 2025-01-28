using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Services
{
    public abstract class BackgroundService
    {
        public abstract event Action<IBackgroundProcess>? OnProcessStarted;
        
        public abstract event Action<IBackgroundProcess>? OnProcessStopped;

        public abstract Task<bool> StartBackgroundProcess(IBackgroundProcess process, int timeout = 5000);

        public abstract T? GetBackgroundProcess<T>() where T : IBackgroundProcess;
        
        public abstract IEnumerable<IBackgroundProcess> GetBackgroundProcesses();
    }
}