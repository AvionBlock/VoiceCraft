using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.ViewModels
{
    [QueryProperty(nameof(StartMode), "startMode")]
    public partial class VoiceViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public VoiceViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public string StartMode { set => Start = bool.Parse(value); }

        [ObservableProperty]
        bool start = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        string passwordInput = string.Empty;

        [ObservableProperty]
        ParticipantModel? selectedParticipant;

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        bool showParticipantVolume = false;

        [ObservableProperty]
        ObservableCollection<ParticipantModel> participants = [];

        [ObservableProperty]
        ObservableCollection<ChannelModel> channels = [];

        [RelayCommand]
        public async Task OnPageAppearing()
        {
            if (!Start && Preferences.Get("VoipServiceRunning", false) == false)
            {
                await _navigationService.GoBack();
                return;
            }

            WeakReferenceMessenger.Default.Register(this, (object recipient, StatusUpdatedMSG message) =>
            {
                StatusText = message.Value;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, StartedSpeakingMSG message) =>
            {
                IsSpeaking = true;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, StoppedSpeakingMSG message) =>
            {
                IsSpeaking = false;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, MutedMSG message) =>
            {
                IsMuted = true;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, UnmutedMSG message) =>
            {
                IsMuted = false;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenedMSG message) =>
            {
                IsDeafened = true;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, UndeafenedMSG message) =>
            {
                IsDeafened = false;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantJoinedMSG message) =>
            {
                if (Participants.FirstOrDefault(x => x.Participant == message.Value) == null)
                {
                    Participants.Add(new ParticipantModel(message.Value));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantLeftMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    Participants.Remove(displayParticipant);
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantUpdatedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    displayParticipant.IsMuted = message.Value.Muted;
                    displayParticipant.IsDeafened = message.Value.Deafened;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantStartedSpeakingMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    displayParticipant.IsSpeaking = true;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantStoppedSpeakingMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    displayParticipant.IsSpeaking = false;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelAddedMSG message) =>
            {
                if (Channels.FirstOrDefault(x => x.Channel == message.Value) == null)
                {
                    Channels.Add(new ChannelModel(message.Value));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelRemovedMSG message) =>
            {
                var channel = Channels.FirstOrDefault(x => x.Channel == message.Value);
                if (channel != null)
                {
                    Channels.Remove(channel);
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelJoinedMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Value);
                if (displayChannel != null)
                {
                    displayChannel.Joined = true;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelLeftMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Value);
                if (displayChannel != null)
                {
                    displayChannel.Joined = false;
                }
            });

            WeakReferenceMessenger.Default.Register(this, async (object recipient, DisconnectedMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Value))
                    await Shell.Current.DisplayAlert("Disconnected!", message.Value, "OK");
                await _navigationService.GoBack();
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DenyMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Value))
                    Shell.Current.DisplayAlert("Denied", message.Value, "OK");
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ResponseDataMSG message) =>
            {
                StatusText = message.Value.StatusMessage;
                IsMuted = message.Value.IsMuted;
                IsDeafened = message.Value.IsDeafened;
                IsSpeaking = message.Value.IsSpeaking;
                Participants = new ObservableCollection<ParticipantModel>(message.Value.Participants);
                Channels = new ObservableCollection<ChannelModel>(message.Value.Channels);
            });

            if (Start)
            {
                WeakReferenceMessenger.Default.Send(new StartServiceMSG());
                Start = false;
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new RequestDataMSG());
            }
        }

        [RelayCommand]
        public void OnPageDisappearing()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        [RelayCommand]
        public async Task Disconnect()
        {
            WeakReferenceMessenger.Default.Send(new DisconnectMSG());
            Preferences.Set("VoipServiceRunning", false);
            await _navigationService.GoBack();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            if(IsMuted)
                WeakReferenceMessenger.Default.Send(new UnmuteMSG());
            else
                WeakReferenceMessenger.Default.Send(new MuteMSG());
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            if (IsDeafened)
                WeakReferenceMessenger.Default.Send(new UndeafenMSG());
            else
                WeakReferenceMessenger.Default.Send(new DeafenMSG());
        }

        [RelayCommand]
        public void ShowHideChannels()
        {
            ShowChannels = !ShowChannels;
        }

        [RelayCommand]
        public void ShowVolume(ParticipantModel participant)
        {
            SelectedParticipant = participant;
            ShowParticipantVolume = true;
        }

        [RelayCommand]
        public void HideVolume()
        {
            ShowParticipantVolume = false;
        }

        [RelayCommand(CanExecute = nameof(CanJoinChannel))]
        public async Task JoinChannel(ChannelModel channel)
        {
            if (channel.Joined)
            {
                WeakReferenceMessenger.Default.Send(new LeaveChannelMSG());
            }
            else
            {
                if (channel.RequiresPassword)
                {
                    var res = await Shell.Current.DisplayPromptAsync("Password", "Please input a password for the channel.", maxLength: 12);
                    if(!string.IsNullOrWhiteSpace(res))
                    {
                        WeakReferenceMessenger.Default.Send(new JoinChannelMSG(new JoinChannel(channel.Channel) { Password = res }));
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new JoinChannelMSG(new JoinChannel(channel.Channel)));
                }
            }
        }

        private bool CanJoinChannel(ChannelModel? channel)
        {
            if (channel?.Channel == null)
                return false;

            if (channel.Joined)
                return true;

            if (channel.Channel.Locked)
                return false;

            return true;
        }
    }
}
