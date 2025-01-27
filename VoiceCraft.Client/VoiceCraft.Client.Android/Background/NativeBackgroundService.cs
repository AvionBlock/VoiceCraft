using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;

namespace VoiceCraft.Client.Android.Background
{
    public class NativeBackgroundService : BackgroundService
    {
        private readonly PermissionsService _permissionsService;
        private readonly ConcurrentQueue<IBackgroundProcess> _queuedProcesses = new();
        public override event Action<IBackgroundProcess>? OnProcessStarted;
        
        public override event Action<IBackgroundProcess>? OnProcessStopped;
        
        public NativeBackgroundService(PermissionsService permissionsService)
        {
            _permissionsService = permissionsService;
            WeakReferenceMessenger.Default.Register<ProcessStarted>(this, (_, m) => OnProcessStarted?.Invoke(m.Value));
            WeakReferenceMessenger.Default.Register<ProcessStopped>(this, (_, m) => OnProcessStopped?.Invoke(m.Value));
            WeakReferenceMessenger.Default.Register<GetQueuedProcess>(this, (_, m) =>
            {
                if(_queuedProcesses.TryDequeue(out var process))
                    m.Reply(process);
            });
        }
        
        private async Task StartService()
        {
            //Is it running?
            if(AndroidBackgroundService.IsStarted) return;
            
            //Don't care if it's granted or not.
            await _permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
                "Notifications are required to show running background processes and errors.");
            
            if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                    "Microphone access is required to properly run the background worker.") != PermissionStatus.Granted) return;

            var context = Application.Context;
            var intent = new Intent(context, typeof(AndroidBackgroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                //Shut the fuck up.
#pragma warning disable CA1416
                context.StartForegroundService(intent);
#pragma warning restore CA1416
            else
                context.StartService(intent);
        }

        public override void StartBackgroundProcess(IBackgroundProcess process)
        {
            _ = StartService();
            _queuedProcesses.Enqueue(process);
        }

        public override T? GetBackgroundProcess<T>() where T : default
        {
            if (!AndroidBackgroundService.IsStarted) return default;
            var processes = GetBackgroundProcesses();
            foreach (var process in processes)
            {
                if(process.GetType() == typeof(T))
                    return (T)process;
            }
            return default;
        }

        public override IEnumerable<IBackgroundProcess> GetBackgroundProcesses()
        {
            if (!AndroidBackgroundService.IsStarted) return [];
            var message = WeakReferenceMessenger.Default.Send<GetBackgroundProcesses>();
            return message.HasReceivedResponse ? message.Response : [];
        }
    }

    //Messages

    public class GetBackgroundProcesses : RequestMessage<IEnumerable<IBackgroundProcess>>;

    public class ProcessStarted(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);
    
    public class ProcessStopped(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);
}