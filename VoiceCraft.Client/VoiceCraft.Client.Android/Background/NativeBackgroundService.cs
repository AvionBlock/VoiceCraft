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
    public class NativeBackgroundService(PermissionsService permissionsService) : BackgroundService
    {
        private async Task StartService()
        {
            //Don't care if it's granted or not.
            await permissionsService.CheckAndRequestPermission<Permissions.PostNotifications>(
                "Notifications are required to show running background processes and errors.");
            
            if (await permissionsService.CheckAndRequestPermission<Permissions.Microphone>(
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

        public override void Test()
        {
            _ = StartService();
        }

        public override void StartBackgroundProcess(IBackgroundProcess process)
        {
            _ = StartService();
            WeakReferenceMessenger.Default.Send(new StartBackgroundProcess(process));
        }

        public override void StopBackgroundProcess<T>()
        {
            throw new System.NotImplementedException();
        }

        public override T GetBackgroundProcess<T>()
        {
            throw new System.NotImplementedException();
        }

        public override IBackgroundProcess[] GetAllBackgroundProcesses()
        {
            throw new System.NotImplementedException();
        }
    }

    //Messages
    public class StartBackgroundProcess(IBackgroundProcess process) : ValueChangedMessage<IBackgroundProcess>(process);
}