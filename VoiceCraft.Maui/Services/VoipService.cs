using NAudio.Wave;
using System.Diagnostics;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.VoiceCraft;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.Services
{
    public class VoipService
    {
        public const int SampleRate = 48000;
        public const int Channels = 1;
        public const int FrameSizeMS = 20;

        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void Deny(string? reason = null);
        public delegate void StatusUpdated(string status);
        public delegate void SpeakingStarted();
        public delegate void SpeakingStopped();
        public delegate void ChannelAdded(Channel channel);
        public delegate void ChannelRemoved(Channel channel);
        public delegate void ChannelJoined(Channel channel);
        public delegate void ChannelLeft(Channel channel);
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant);
        public delegate void ParticipantUpdated(VoiceCraftParticipant participant);
        public delegate void ParticipantStartedSpeaking(VoiceCraftParticipant participant);
        public delegate void ParticipantStoppedSpeaking(VoiceCraftParticipant participant);


        #region Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event Deny? OnDeny;
        public event StatusUpdated? OnStatusUpdated;
        public event SpeakingStarted? OnSpeakingStarted;
        public event SpeakingStopped? OnSpeakingStopped;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelRemoved? OnChannelRemoved;
        public event ChannelJoined? OnChannelJoined;
        public event ChannelLeft? OnChannelLeft;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantUpdated? OnParticipantUpdated;
        public event ParticipantStartedSpeaking? OnParticipantStartedSpeaking;
        public event ParticipantStoppedSpeaking? OnParticipantStoppedSpeaking;
        #endregion

        #region Fields
        public SettingsModel Settings { get; private set; }
        public ServerModel Server { get; private set; }
        public VoiceCraftClient Client { get; private set; }
        public string StatusMessage { get; private set; } = string.Empty;
        public string Username { get; private set; } = string.Empty;

        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;
        private SoftLimiter Normalizer;
        private long RecordDetection = 0;
        #endregion

        public VoipService(ServerModel server, IDatabaseService databaseService)
        {
            Settings = databaseService.Settings;
            Server = server;

            Client = new VoiceCraftClient(new WaveFormat(SampleRate, Channels), FrameSizeMS, Settings.ClientPort, Settings.JitterBufferSize, Settings.Bitrate, Settings.UseDtx)
            {
                LinearProximity = Settings.LinearVolume,
                UseCustomProtocol = Settings.CustomClientProtocol,
                DirectionalHearing = Settings.DirectionalAudioEnabled
            };

            Client.OnConnected += ClientConnected;
            Client.OnDisconnected += ClientDisconnected;
            Client.OnFailed += ClientFailed;
            Client.OnDeny += ClientDeny;
            Client.OnBinded += ClientBinded;
            Client.OnUnbinded += ClientUnbinded;
            Client.OnChannelAdded += ClientChannelAdded;
            Client.OnChannelRemoved += ClientChannelRemoved;
            Client.OnChannelJoined += ClientChannelJoined;
            Client.OnChannelLeft += ClientChannelLeft;
            Client.OnParticipantJoined += ClientParticipantJoined;
            Client.OnParticipantLeft += ClientParticipantLeft;
            Client.OnParticipantUpdated += ClientParticipantUpdated;
        }

        public async Task StartAsync(CancellationToken CT)
        {
            await Task.Run(async () =>
            {
                var audioManager = new AudioManager(Settings);

                if (Settings.SoftLimiterEnabled)
                {
                    Normalizer = new SoftLimiter(Client.AudioOutput);
                    Normalizer.Boost.CurrentValue = Settings.SoftLimiterGain;
                    AudioPlayer = audioManager.CreatePlayer(Normalizer);
                }
                else
                {
                    AudioPlayer = audioManager.CreatePlayer(Client.AudioOutput);
                }
                AudioRecorder = audioManager.CreateRecorder(Client.AudioFormat, FrameSizeMS);

                AudioRecorder.DataAvailable += DataAvailable;
                AudioRecorder.RecordingStopped += RecordingStopped;

                try
                {
                    Client.Connect(Server.IP, (ushort)Server.Port, Server.Key, Settings.ClientSidedPositioning ? Core.PositioningTypes.ClientSided : Core.PositioningTypes.ServerSided);
                    await StartLogicLoop(CT);
                    if (AudioPlayer.PlaybackState != PlaybackState.Playing) AudioPlayer.Play();
                }
                catch (OperationCanceledException)
                { }
                catch (Exception ex)
                {
                    OnStopped?.Invoke(ex.Message);
                }
                finally
                {
                    Client.OnConnected -= ClientConnected;
                    Client.OnDisconnected -= ClientDisconnected;
                    Client.OnFailed -= ClientFailed;
                    Client.OnDeny -= ClientDeny;
                    Client.OnBinded -= ClientBinded;
                    Client.OnUnbinded -= ClientUnbinded;
                    Client.OnChannelAdded -= ClientChannelAdded;
                    Client.OnChannelRemoved -= ClientChannelRemoved;
                    Client.OnChannelJoined -= ClientChannelJoined;
                    Client.OnChannelLeft -= ClientChannelLeft;
                    Client.OnParticipantJoined -= ClientParticipantJoined;
                    Client.OnParticipantLeft -= ClientParticipantLeft;
                    Client.OnParticipantUpdated -= ClientParticipantUpdated;
                    AudioRecorder.DataAvailable -= DataAvailable;
                    AudioRecorder.RecordingStopped -= RecordingStopped;

                    if (AudioPlayer.PlaybackState == PlaybackState.Playing)
                        AudioPlayer.Stop();

                    AudioRecorder.StopRecording();
                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();

                    await Client.DisconnectAsync();
                    Client.Dispose();
                }
            }, CT);
        }

        private async Task StartLogicLoop(CancellationToken CT)
        {
            OnStarted?.Invoke();
            var talkingParticipants = new List<VoiceCraftParticipant>();
            bool previousSpeakingState = false;
            while (true)
            {
                await Task.Delay(200, CT);
                try
                {
                    var currentSpeakingState = Environment.TickCount64 - RecordDetection < 500;
                    if (previousSpeakingState != currentSpeakingState)
                    {
                        if(currentSpeakingState)
                            OnSpeakingStarted?.Invoke();
                        else
                            OnSpeakingStopped?.Invoke();
                        previousSpeakingState = currentSpeakingState;
                    }

                    //Participant Talking Logic.
                    var oldPart = talkingParticipants.Where(x => Environment.TickCount64 - x.LastSpoke >= 500).ToArray();
                    foreach (var participant in oldPart)
                    {
                        talkingParticipants.Remove(participant);
                        OnParticipantStoppedSpeaking?.Invoke(participant);
                    }

                    var newPart = Client.Participants.Where(x => 
                        Environment.TickCount64 - x.Value.LastSpoke < 500 && 
                        !talkingParticipants.Contains(x.Value));
                    foreach (var participant in newPart)
                    {
                        talkingParticipants.Add(participant.Value);
                        OnParticipantStartedSpeaking?.Invoke(participant.Value);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private float prevIn = 0;
        private float prevOut = 0;

        //Audio Events
        private void DataAvailable(object? sender, WaveInEventArgs e)
        {
            var client = Client; // Cache locally for thread-safety
            if (client == null) return;
            if (client.Muted || client.Deafened)
                return;

            // Guard against odd bytes (16-bit audio requires even bytes)
            int bytesToProcess = e.BytesRecorded - (e.BytesRecorded % 2);

            // Noise Suppression (High Pass Filter 80Hz)
            if (Settings.NoiseSuppression)
            {
                float alpha = 0.989f;
                for (int i = 0; i < bytesToProcess; i += 2)
                {
                    // Ensure buffer safety
                    if (i + 1 >= e.Buffer.Length) break;

                    short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                    float input = sample;
                    float output = alpha * (prevOut + input - prevIn);
                    
                    prevIn = input;
                    prevOut = output;
                    
                    short outSample = (short)Math.Clamp(output, short.MinValue, short.MaxValue);
                    e.Buffer[i] = (byte)(outSample & 0xFF);
                    e.Buffer[i + 1] = (byte)(outSample >> 8);
                }
            }

            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < bytesToProcess; index += 2)
            {
                 // Ensure buffer safety
                if (index + 1 >= e.Buffer.Length) break;

                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            if (max >= Settings.MicrophoneDetectionPercentage)
            {
                RecordDetection = Environment.TickCount64;
            }

            if (Environment.TickCount64 - RecordDetection < 1000)
            {
                client.SendAudio(e.Buffer, bytesToProcess);
            }
        }

        private void RecordingStopped(object? sender, StoppedEventArgs e)
        {
            AudioRecorder?.StartRecording();
        }

        #region Event Methods
        private void ClientConnected()
        {
            if(Client.PositioningType == PositioningTypes.ServerSided)
            {
                StatusMessage = $"Connected! Key - {Client.Key}\nWaiting for binding...";
            }
            else if(Settings.CustomClientProtocol)
            {
                StatusMessage = $"Connected!\nWaiting for client on port {Settings.ClientPort}";
            }
            else
            {
                StatusMessage = $"Connected!\nWaiting for MCWSS on port {Settings.ClientPort}";
            }
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void ClientDisconnected(string? reason = null)
        {
            OnStopped?.Invoke(reason);
        }

        private void ClientFailed(Exception ex)
        {
            OnStopped?.Invoke(ex.Message);
        }

        private void ClientDeny(string? reason = null)
        {
            OnDeny?.Invoke(reason);
        }

        private void ClientBinded(string name)
        {
            Username = name ?? "<N.A.>";
            StatusMessage = Client.PositioningType == PositioningTypes.ServerSided? $"Connected - Key: {Client.Key}\n{Username}" : $"Connected\n{Username}";

            //Last step of verification. We start sending data and playing any received data.
            try
            {
                AudioRecorder?.StartRecording();
                AudioPlayer?.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            } //Do nothing. This is just to make sure that the recorder and player is working.
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void ClientUnbinded()
        {
            if (Settings.CustomClientProtocol)
            {
                StatusMessage = $"Connected! Key\nDisconnected connection!";
            }
            else
            {
                StatusMessage = $"Connected! Key\nMCWSS Disconnected!";
            }
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void ClientChannelAdded(Core.Channel channel)
        {
            OnChannelAdded?.Invoke(channel);
        }

        private void ClientChannelRemoved(Core.Channel channel)
        {
            OnChannelRemoved?.Invoke(channel);
        }

        private void ClientChannelJoined(Core.Channel channel)
        {
            OnChannelJoined?.Invoke(channel);
        }

        private void ClientChannelLeft(Core.Channel channel)
        {
            OnChannelLeft?.Invoke(channel);
        }

        private void ClientParticipantJoined(VoiceCraftParticipant participant)
        {
            OnParticipantJoined?.Invoke(participant);
        }

        private void ClientParticipantLeft(VoiceCraftParticipant participant)
        {
            OnParticipantLeft?.Invoke(participant);
        }

        private void ClientParticipantUpdated(VoiceCraftParticipant participant)
        {
            OnParticipantUpdated?.Invoke(participant);
        }
        #endregion
    }
}
