using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.VoiceCraft;

namespace VoiceCraft.Maui
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class ServiceManager : Service, IServiceManager
    {
        private CancellationTokenSource? Cts;
        private VoipService? VoipService;
        private Task? easterEggLoop;

        const string NOTIFICATION_CHANNEL_ID = "1000";
        const int NOTIFICATION_ID = 1;
        const string NOTIFICATION_CHANNEL_NAME = "Voice";

        readonly string[] splashEasterEggs = [
            "1+1 = window.",
            "creeper, aww man.", 
            "ANDROID FTW.",
            "PC master race.", 
            "No way dude, That's insane.",
            "What came first, the chicken or the egg?",
            "Android version has easter eggs. Or does it?",
            "The wheel's on a bus go... oh nevermind.",
            "Press F for help.",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            $"{Math.PI}",
            "Baby shark do doo do doo do do"
        ];

        public void StartService()
        {
            try
            {
                Preferences.Set("VoipServiceRunning", true);
                var serviceIntent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
                Android.App.Application.Context.StartForegroundService(serviceIntent);
            }
            catch
            {
                Preferences.Set("VoipServiceRunning", false);
            }
        }

        public static void Stop() 
        {
            try
            {
                Preferences.Set("VoipServiceRunning", false);
                var serviceIntent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
                Android.App.Application.Context.StopService(serviceIntent);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private NotificationCompat.Builder CreateNotification(NotificationManager notificationManager)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Low);
                notificationManager?.CreateNotificationChannel(channel);
            }

            var nBuilder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Starting...")
                .SetContentText("Starting VoiceCraft...")
                .SetSmallIcon(Resource.Drawable.microphone)
                .SetOngoing(true);

            return nBuilder;
        }

        private void StartVoiceCraftService()
        {
            var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager ?? throw new Exception("Notification manager was null");
            StartForeground(NOTIFICATION_ID, CreateNotification(notificationManager).Build());
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            StartVoiceCraftService();
            Cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    VoipService = new VoipService(Navigator.GetNavigationData<ServerModel>());

                    VoipService.OnStatusUpdated += StatusUpdated;
                    VoipService.OnSpeakingStarted += SpeakingStarted;
                    VoipService.OnSpeakingStopped += SpeakingStopped;
                    VoipService.OnParticipantJoined += ParticipantJoined;
                    VoipService.OnParticipantLeft += ParticipantLeft;
                    VoipService.OnParticipantUpdated += ParticipantUpdated;
                    VoipService.OnParticipantStartedSpeaking += ParticipantStartedSpeaking;
                    VoipService.OnParticipantStoppedSpeaking += ParticipantStoppedSpeaking;
                    VoipService.OnChannelAdded += ChannelAdded;
                    VoipService.OnChannelRemoved += ChannelRemoved;
                    VoipService.OnChannelJoined += ChannelJoined;
                    VoipService.OnChannelLeft += ChannelLeft;
                    VoipService.OnStopped += Stopped;
                    VoipService.OnDeny += Deny;

                    WeakReferenceMessenger.Default.Register(this, (object recipient, RequestDataMSG message) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            WeakReferenceMessenger.Default.Send(new ResponseDataMSG(new ResponseData(
                                VoipService.Client.Participants.Select(x => new ParticipantModel(x.Value)).ToList(),
                                VoipService.Client.Channels.Select(x => new ChannelModel(x.Value) { Joined = x.Value == VoipService.Client.JoinedChannel }).ToList(),
                                false,
                                VoipService.Client.Muted,
                                VoipService.Client.Deafened,
                                VoipService.StatusMessage)));
                        });
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, MuteMSG message) =>
                    {
                        try
                        {
                            VoipService.Client.SetMute(true);
                            WeakReferenceMessenger.Default.Send(new MutedMSG());
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, UnmuteMSG message) =>
                    {
                        try
                        {
                            VoipService.Client.SetMute(false);
                            WeakReferenceMessenger.Default.Send(new UnmutedMSG());
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenMSG message) =>
                    {
                        try
                        {
                            VoipService.Client.SetDeafen(true);
                            WeakReferenceMessenger.Default.Send(new DeafenedMSG());
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, UndeafenMSG message) =>
                    {
                        try
                        {
                            VoipService.Client.SetDeafen(false);
                            WeakReferenceMessenger.Default.Send(new UndeafenedMSG());
                        }
                        catch (InvalidOperationException)
                        {
                            return;
                        }
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DisconnectMSG message) =>
                    {
                        Cts.Cancel();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, JoinChannelMSG message) =>
                    {
                        VoipService.Client.JoinChannel(message.Value.Channel, message.Value.Password);
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, LeaveChannelMSG message) =>
                    {
                        VoipService.Client.LeaveChannel();
                    });

                    easterEggLoop = Task.Run(async () => {
                        while(true)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(2));
                            var random = new Random();

                            if (!Cts.IsCancellationRequested && VoipService != null)
                            {
                                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                                notificationManager?.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService.StatusMessage).SetContentText(splashEasterEggs[random.Next(0, splashEasterEggs.Length - 1)]).Build());
                            }
                            else
                            {
                                break;
                            }
                        }
                    }, Cts.Token);

                    VoipService.StartAsync(Cts.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    if (VoipService != null)
                    {
                        VoipService.OnStatusUpdated -= StatusUpdated;
                        VoipService.OnSpeakingStarted -= SpeakingStarted;
                        VoipService.OnSpeakingStopped -= SpeakingStopped;
                        VoipService.OnParticipantJoined -= ParticipantJoined;
                        VoipService.OnParticipantLeft -= ParticipantLeft;
                        VoipService.OnParticipantUpdated -= ParticipantUpdated;
                        VoipService.OnParticipantStartedSpeaking -= ParticipantStartedSpeaking;
                        VoipService.OnParticipantStoppedSpeaking -= ParticipantStoppedSpeaking;
                        VoipService.OnChannelAdded -= ChannelAdded;
                        VoipService.OnChannelRemoved -= ChannelRemoved;
                        VoipService.OnChannelJoined -= ChannelJoined;
                        VoipService.OnChannelLeft -= ChannelLeft;
                        VoipService.OnStopped -= Stopped;
                        VoipService.OnDeny -= Deny;
                    }

                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    Stop();
                    Cts.Dispose();
                    easterEggLoop = null;
                }
            }, Cts.Token);
            return StartCommandResult.NotSticky;
        }

        public override void OnDestroy()
        {
            try
            {
                Preferences.Set("VoipServiceRunning", false); //Set to false anyways.
                easterEggLoop = null;
                if (!Cts?.IsCancellationRequested ?? false)
                {
                    Cts?.Cancel();
                }
            }
            catch (System.OperationCanceledException)
            {
                //Do nothing
            }
            base.OnDestroy();
        }

        private void StatusUpdated(string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(status).SetContentText("Idling...").Build());

                WeakReferenceMessenger.Default.Send(new StatusUpdatedMSG(status));
            });
        }

        private void SpeakingStarted()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new StartedSpeakingMSG());
            });
        }

        private void SpeakingStopped()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new StoppedSpeakingMSG());
            });
        }

        private void ParticipantJoined(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService?.StatusMessage ?? "Disconnected").SetContentText($"{participant.Name} has connected!").Build());

                WeakReferenceMessenger.Default.Send(new ParticipantJoinedMSG(participant));
            });
        }

        private void ParticipantLeft(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager?.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService?.StatusMessage ?? "Disconnected").SetContentText($"{participant.Name} has disconnected!").Build());

                WeakReferenceMessenger.Default.Send(new ParticipantLeftMSG(participant));
            });
        }

        private void ParticipantUpdated(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantUpdatedMSG(participant));
            });
        }

        private void ParticipantStartedSpeaking(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantStartedSpeakingMSG(participant));
            });
        }

        private void ParticipantStoppedSpeaking(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantStoppedSpeakingMSG(participant));
            });
        }

        private void ChannelAdded(Core.Channel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelAddedMSG(channel));
            });
        }

        private void ChannelRemoved(Core.Channel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelRemovedMSG(channel));
            });
        }

        private void ChannelJoined(Core.Channel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelJoinedMSG(channel));
            });
        }

        private void ChannelLeft(Core.Channel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelLeftMSG(channel));
            });
        }

        private void Stopped(string? reason = null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new DisconnectedMSG(reason ?? string.Empty));
            });
            Cts?.Cancel();
        }

        private void Deny(string? reason = null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new DenyMSG(reason ?? string.Empty));
            });
        }
    }
}
