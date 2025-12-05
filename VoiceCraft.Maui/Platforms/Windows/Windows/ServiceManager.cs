using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.VoiceCraft;

namespace VoiceCraft.Maui
{
    public class ServiceManager : IServiceManager
    {
        private readonly CancellationTokenSource Cts;
        private readonly VoipService VoipService;

        public ServiceManager()
        {
            Cts = new CancellationTokenSource();
            VoipService = new VoipService(Navigator.GetNavigationData<ServerModel>(), IPlatformApplication.Current.Services.GetService<IDatabaseService>());

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
        }

        public void StartService()
        {
            Task.Run(() =>
            {
                try
                {
                    Preferences.Set("VoipServiceRunning", true);
                    WeakReferenceMessenger.Default.Register(this, (object recipient, RequestDataMSG message) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            WeakReferenceMessenger.Default.Send(new ResponseDataMSG(new ResponseData(
                                VoipService.Client.Participants.Select(x => new ParticipantModel(x.Value)).ToList(),
                                VoipService.Client.Channels.Select(x => new ChannelModel(x.Value) { Joined = x.Value == VoipService.Client.JoinedChannel}).ToList(),
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

                    VoipService.StartAsync(Cts.Token).Wait();
                }
                catch (OperationCanceledException)
                {
                }
                finally
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

                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    Cts.Dispose();
                    Preferences.Set("VoipServiceRunning", false);
                }
            }, Cts.Token);
        }

        private void StatusUpdated(string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
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
                WeakReferenceMessenger.Default.Send(new ParticipantJoinedMSG(participant));
            });
        }

        private void ParticipantLeft(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
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
