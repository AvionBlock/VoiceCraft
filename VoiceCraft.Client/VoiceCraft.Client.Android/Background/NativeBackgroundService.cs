using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Maui.ApplicationModel;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Background;

public class NativeBackgroundService : BackgroundService
{
    private readonly PermissionsService _permissionsService;

    public NativeBackgroundService(PermissionsService permissionsService)
    {
        _permissionsService = permissionsService;
        WeakReferenceMessenger.Default.Register<ProcessStarted>(this, (_, m) => OnProcessStarted?.Invoke(m.Value));
        WeakReferenceMessenger.Default.Register<ProcessStopped>(this, (_, m) => OnProcessStopped?.Invoke(m.Value));
    }

    public override event Action<IBackgroundProcess>? OnProcessStarted;

    public override event Action<IBackgroundProcess>? OnProcessStopped;

    public override async Task StartBackgroundProcess<T>(T process, int timeout = 5000)
    {
        var processType = typeof(T);
        if (AndroidBackgroundService.Processes.ContainsKey(processType))
            throw new InvalidOperationException("A background process of this type has already been queued/started!");

        var backgroundProcess = new BackgroundProcess(process);
        AndroidBackgroundService.Processes.TryAdd(processType, backgroundProcess);
        if (!await StartBackgroundWorker())
        {
            AndroidBackgroundService.Processes.Clear();
            throw new Exception("Failed to start background process! Background worker failed to start!");
        }

        var startTime = DateTime.UtcNow;
        while (backgroundProcess.Status == BackgroundProcessStatus.Stopped)
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds >= timeout)
            {
                AndroidBackgroundService.Processes.TryRemove(processType, out _);
                backgroundProcess.Dispose();
                throw new Exception("Failed to start background process!");
            }

            await Task.Delay(10); //Don't burn the CPU!
        }
    }

    public override Task StopBackgroundProcess<T>()
    {
        var processType = typeof(T);
        if (!AndroidBackgroundService.Processes.TryRemove(processType, out var process)) return Task.CompletedTask;
        process.Stop();
        process.Dispose();
        OnProcessStopped?.Invoke(process.Process);
        return Task.CompletedTask;
    }

    public override bool TryGetBackgroundProcess<T>(out T? process) where T : default
    {
        var processType = typeof(T);
        if (!AndroidBackgroundService.Processes.TryGetValue(processType, out var value))
        {
            process = default;
            return false;
        }

        process = (T?)value.Process;
        return process != null;
    }

    private async Task<bool> StartBackgroundWorker()
    {
        //Is it running?
        if (AndroidBackgroundService.IsStarted) return true;

        //Don't care if it's granted or not.
        await _permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
            "Notifications are required to show running background processes and errors.");

        if (await _permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
                "Microphone access is required to properly run the background worker.") != PermissionStatus.Granted) return false;

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
}

//Messages
public class ProcessStarted(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);

public class ProcessStopped(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);