using System;
using System.Collections.Generic;
using System.Linq;
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
        public override event Action<IBackgroundProcess>? OnProcessStarted;

        public override event Action<IBackgroundProcess>? OnProcessStopped;

        public NativeBackgroundService(PermissionsService permissionsService)
        {
            _permissionsService = permissionsService;
            WeakReferenceMessenger.Default.Register<ProcessStarted>(this, (_, m) => OnProcessStarted?.Invoke(m.Value));
            WeakReferenceMessenger.Default.Register<ProcessStopped>(this, (_, m) => OnProcessStopped?.Invoke(m.Value));
        }

        protected override bool StartBackgroundWorker()
        {
            //Is it running?
            if (AndroidBackgroundService.IsStarted) return true;

            //Don't care if it's granted or not.
            _permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
                "Notifications are required to show running background processes and errors.").GetAwaiter().GetResult();

            if (_permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                    "Microphone access is required to properly run the background worker.").GetAwaiter().GetResult() != PermissionStatus.Granted) return false;

            var context = Application.Context;
            var intent = new Intent(context, typeof(AndroidBackgroundService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                //Shut the fuck up.
#pragma warning disable CA1416
                context.StartForegroundService(intent);
#pragma warning restore CA1416
            else
                context.StartService(intent);

            return true;
        }

        public override async Task StartBackgroundProcess<T>(T process, int timeout = 5000)
        {
            var processType = typeof(T);
            if (AndroidBackgroundService.QueuedProcesses.Any(x => x.Key == processType) ||
                AndroidBackgroundService.RunningBackgroundProcesses.ContainsKey(processType))
                throw new InvalidOperationException("A background process of this type has already been queued/started!");

            AndroidBackgroundService.QueuedProcesses.Enqueue(new KeyValuePair<Type, IBackgroundProcess>(processType, process));
            if (!StartBackgroundWorker())
            {
                AndroidBackgroundService.QueuedProcesses.Clear();
                throw new Exception("Failed to start background process! Background worker failed to start!");
            }

            var startTime = System.Environment.TickCount;
            while (!AndroidBackgroundService.RunningBackgroundProcesses.ContainsKey(processType))
            {
                if (System.Environment.TickCount - startTime >= timeout)
                    throw new Exception("Failed to start background process!");
                await Task.Delay(10); //Don't burn the CPU!
            }
        }

        public override Task StopBackgroundProcess<T>()
        {
            var processType = typeof(T);
            var message = WeakReferenceMessenger.Default.Send(new StopBackgroundProcess(processType));
            if (message is { HasReceivedResponse: true, Response: not null })
            {
                OnProcessStopped?.Invoke(message.Response);
            }
            
            return Task.CompletedTask;
        }
        
        public override bool TryGetBackgroundProcess<T>(out T? process) where T : default
        {
            var processType = typeof(T);
            if (!AndroidBackgroundService.RunningBackgroundProcesses.TryGetValue(processType, out var value))
            {
                process = default;
                return false;
            }
            
            process = (T?)value.Value;
            return process != null;
        }
    }

    //Messages
    public class StopBackgroundProcess(Type processType) : RequestMessage<IBackgroundProcess?>
    {
        public readonly Type ProcessType = processType;
    }

    public class ProcessStarted(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);

    public class ProcessStopped(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);
}