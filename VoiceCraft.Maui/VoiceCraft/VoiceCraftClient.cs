using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpusSharp.Core;
using System.Collections.Concurrent;
using System.Net.Sockets;
using VoiceCraft.Core;

namespace VoiceCraft.Maui.VoiceCraft
{
    public class VoiceCraftClient : Core.Disposable
    {
        //Private Variables
        // Keep client version aligned with server/Core constants to prevent login denial.
        public const string Version = Core.Constants.Version;
        private ConnectionState State;
        private uint PacketCount;
        private readonly OpusEncoder Encoder;
        private readonly int FrameSizeMS;
        private readonly int ClientPort;
        private readonly int JitterBufferSize;
        private string EnvironmentId = string.Empty;

        //Variables
        public ConcurrentDictionary<short, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<short, VoiceCraftParticipant>();
        public ConcurrentDictionary<byte, Channel> Channels { get; set; } = new ConcurrentDictionary<byte, Channel>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; } = new Network.Sockets.VoiceCraft();
        public Network.Sockets.CustomClient CustomClient { get; set; }
        public Network.Sockets.MCWSS MCWSS { get; set; }
        public short Key { get; private set; }
        public Channel? JoinedChannel { get; private set; }
        public PositioningTypes PositioningType { get; private set; }

        //Audio Variables
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }
        public bool LinearProximity { get; set; }
        public bool DirectionalHearing { get; set; }
        public bool UseCustomProtocol { get; set; }
        public MixingSampleProvider AudioOutput { get; }
        public WaveFormat AudioFormat { get; }
        public WaveFormat PlaybackFormat { get; }

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);
        public delegate void Deny(string? reason = null);
        public delegate void Failed(Exception ex);

        public delegate void Binded(string name);
        public delegate void Unbinded();
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant);
        public delegate void ParticipantUpdated(VoiceCraftParticipant participant);
        public delegate void ChannelAdded(Channel channel);
        public delegate void ChannelRemoved(Channel channel);
        public delegate void ChannelJoined(Channel channel);
        public delegate void ChannelLeft(Channel channel);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event Deny? OnDeny;
        public event Failed? OnFailed;

        public event Binded? OnBinded;
        public event Unbinded? OnUnbinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantUpdated? OnParticipantUpdated;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelRemoved? OnChannelRemoved;
        public event ChannelJoined? OnChannelJoined;
        public event ChannelLeft? OnChannelLeft;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat, int frameSizeMS = 20, int ClientPort = 8080, int jitterBufferSize = 80, int bitrate = 16000, bool useDtx = true)
        {
            this.ClientPort = ClientPort;
            MCWSS = new Network.Sockets.MCWSS(ClientPort);
            CustomClient = new Network.Sockets.CustomClient();

            AudioFormat = audioFormat;
            PlaybackFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioFormat.SampleRate, 2);
            FrameSizeMS = frameSizeMS;
            JitterBufferSize = jitterBufferSize;

            Encoder = new OpusEncoder(AudioFormat.SampleRate, AudioFormat.Channels, OpusSharp.Core.Enums.PreDefCtl.OPUS_APPLICATION_VOIP)
            {
                Bitrate = bitrate,
                //SignalType = OpusSharp.Core.Enums.PreDefCtl.OPUS_SIGNAL_VOICE,
                //UseDTX = useDtx, 
                PacketLossPerc = 20 // 20% is more reasonable than 50%
            };
            AudioOutput = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };

            MCWSS.OnConnected += MCWSSConnected;
            MCWSS.OnFailed += MCWSSFailed;
            MCWSS.OnDisconnected += MCWSSDisconnected;
            MCWSS.OnPlayerTravelled += MCWSSPlayerTravelled;

            CustomClient.OnConnected += CustomClientConnected;
            CustomClient.OnDisconnected += CustomClientDisconnected;
            CustomClient.OnFailed += CustomClientFailed;
            CustomClient.OnUpdated += CustomClientUpdated;

            VoiceCraftSocket.OnConnected += VoiceCraftSocketConnected;
            VoiceCraftSocket.OnDisconnected += VoiceCraftSocketDisconnected;
            VoiceCraftSocket.OnBindedReceived += VoiceCraftSocketBinded;
            VoiceCraftSocket.OnParticipantLeftReceived += VoiceCraftSocketParticipantLeft;
            VoiceCraftSocket.OnParticipantJoinedReceived += VoiceCraftSocketParticipantJoined;
            VoiceCraftSocket.OnAddChannelReceived += VoiceCraftSocketAddChannel;
            VoiceCraftSocket.OnRemoveChannelReceived += VoiceCraftSocketRemoveChannel;
            VoiceCraftSocket.OnMuteReceived += VoiceCraftSocketMute;
            VoiceCraftSocket.OnUnmuteReceived += VoiceCraftSocketUnmute;
            VoiceCraftSocket.OnDeafenReceived += VoiceCraftSocketDeafen;
            VoiceCraftSocket.OnUndeafenReceived += VoiceCraftSocketUndeafen;
            VoiceCraftSocket.OnJoinChannelReceived += VoiceCraftSocketJoinChannel;
            VoiceCraftSocket.OnLeaveChannelReceived += VoiceCraftSocketLeaveChannel;
            VoiceCraftSocket.OnServerAudioReceived += VoiceCraftSocketServerAudio;
            VoiceCraftSocket.OnDenyReceived += VoiceCraftSocketDenyReceived;
        }

        #region Methods
        public void Connect(string ip, ushort port, short key, PositioningTypes positioningType)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftClient));
            if (State ==  ConnectionState.Connected || State == ConnectionState.Connecting) return;

            PositioningType = positioningType;
            State = ConnectionState.Connecting;
            _ = Task.Run(async () =>
            {
                try
                {
                    await VoiceCraftSocket.ConnectAsync(ip, port, key, positioningType, Version);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    OnFailed?.Invoke(ex);
                }
            });
        }

        public async Task DisconnectAsync(string? reason = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed && State == ConnectionState.Disconnected, nameof(VoiceCraftClient));
            if (State == ConnectionState.Disconnected || State == ConnectionState.Disconnecting) return;
            State = ConnectionState.Disconnecting;

            await VoiceCraftSocket.DisconnectAsync();
            await CustomClient.StopAsync();
            MCWSS.Stop();
            ClearParticipants();
            ClearChannels();
            State = ConnectionState.Disconnected;
            OnDisconnected?.Invoke(reason);
        }

        public void SetMute(bool mute)
        {
            if(mute != Muted)
            {
                Core.Packets.VoiceCraftPacket packet = mute ? new Core.Packets.VoiceCraft.Mute() : new Core.Packets.VoiceCraft.Unmute();
                VoiceCraftSocket.Send(packet);
                Muted = mute;
            }
        }

        public void SetDeafen(bool deafen)
        {
            if (deafen != Deafened)
            {
                Deafened = deafen;
                Core.Packets.VoiceCraftPacket packet = deafen ? new Core.Packets.VoiceCraft.Deafen() : new Core.Packets.VoiceCraft.Undeafen();
                VoiceCraftSocket.Send(packet);
            }
        }

        public void JoinChannel(Channel channel, string password = "")
        {
            var c = Channels.FirstOrDefault(x => x.Value == channel);
            if (c.Value != null && JoinedChannel != channel)
            {
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = c.Key, Password = password });
            }
        }

        public void LeaveChannel()
        {
            var c = Channels.FirstOrDefault(x => x.Value == JoinedChannel);
            if (c.Value != null && JoinedChannel != null)
            {
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.LeaveChannel());
            }
        }

        private readonly byte[] _audioEncodeBuffer = new byte[1000]; // Reusable buffer for encoding
        private readonly object _audioLock = new object();

        public void SendAudio(byte[] audio, int bytesRecorded)
        {
            if (Deafened || Muted || State != ConnectionState.Connected) return;

            lock (_audioLock)
            {
                PacketCount++;
                int encodedBytes = Encoder.Encode(audio, bytesRecorded, _audioEncodeBuffer);
                
                // Memory Optimization: Use ArrayPool to avoid allocating new byte[] every 20ms
                byte[] audioBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(encodedBytes);
                Buffer.BlockCopy(_audioEncodeBuffer, 0, audioBuffer, 0, encodedBytes);

                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.ClientAudio() 
                { 
                    Audio = audioBuffer, 
                    DataLength = encodedBytes,
                    PacketCount = PacketCount 
                });
                
                // Note: Array is returned to pool when packet is disposed by VoiceCraftSocket sender loop.
            }
        }

        private void ClearParticipants()
        {
            foreach (var participant in Participants)
            {
                OnParticipantLeft?.Invoke(participant.Value);
                participant.Value.Dispose();
            }
            Participants.Clear();
        }

        private void ClearChannels()
        {
            foreach(var channel in Channels)
            {
                OnChannelRemoved?.Invoke(channel.Value);
            }
            Channels.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                // Avoid blocking the calling thread; best-effort async cleanup.
                if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
                {
                    try { DisconnectAsync().Wait(3000); } catch { }
                }

                try { VoiceCraftSocket.Dispose(); } catch { }
                try { MCWSS.Dispose(); } catch { }
                try { CustomClient.Dispose(); } catch { }
                try { Encoder.Dispose(); } catch { }
                Channels.Clear();
                Participants.Clear();
            }
        }
        #endregion

        #region Event Methods
        private void VoiceCraftSocketConnected(short key)
        {
            Key = key;
            State = ConnectionState.Connected;
            OnConnected?.Invoke();

            if (PositioningType == PositioningTypes.ClientSided)
            {
                if (UseCustomProtocol)
                    _ = Task.Run(async () => await CustomClient.HostAsync(ClientPort));
                else
                    MCWSS.Start();
            }
        }
        
        private async void VoiceCraftSocketDisconnected(string? reason = null)
        {
            await DisconnectAsync(reason);
        }

        private void VoiceCraftSocketBinded(Core.Packets.VoiceCraft.Binded data, Network.NetPeer peer)
        {
            OnBinded?.Invoke(data.Name);
        }

        private void VoiceCraftSocketParticipantJoined(Core.Packets.VoiceCraft.ParticipantJoined data, Network.NetPeer peer)
        {
            var participant = new VoiceCraftParticipant(data.Name, AudioFormat, FrameSizeMS, JitterBufferSize) { Deafened = data.IsDeafened, Muted = data.IsMuted };
            if (Participants.TryAdd(data.Key, participant))
            {
                AudioOutput.AddMixerInput(participant.AudioOutput);
                OnParticipantJoined?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketParticipantLeft(Core.Packets.VoiceCraft.ParticipantLeft data, Network.NetPeer peer)
        {
            if(Participants.TryRemove(data.Key, out var participant))
            {
                AudioOutput.RemoveMixerInput(participant.AudioOutput);
                OnParticipantLeft?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketAddChannel(Core.Packets.VoiceCraft.AddChannel data, Network.NetPeer peer)
        {
            var channel = new Channel() { Name = data.Name, Locked = data.Locked, Password = data.RequiresPassword ? "Required" : string.Empty };
            if(Channels.TryAdd(data.ChannelId, channel))
            {
                OnChannelAdded?.Invoke(channel);
            }
        }

        private void VoiceCraftSocketRemoveChannel(Core.Packets.VoiceCraft.RemoveChannel data, Network.NetPeer peer)
        {
            if(Channels.TryRemove(data.ChannelId, out var channel))
            {
                OnChannelRemoved?.Invoke(channel);
            }
        }

        private void VoiceCraftSocketMute(Core.Packets.VoiceCraft.Mute data, Network.NetPeer peer)
        {
            if(Participants.TryGetValue(data.Key, out var participant) && !participant.Muted)
            {
                participant.Muted = true;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketUnmute(Core.Packets.VoiceCraft.Unmute data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Muted)
            {
                participant.Muted = false;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketDeafen(Core.Packets.VoiceCraft.Deafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && !participant.Deafened)
            {
                participant.Deafened = true;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketUndeafen(Core.Packets.VoiceCraft.Undeafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Deafened)
            {
                participant.Deafened = false;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketJoinChannel(Core.Packets.VoiceCraft.JoinChannel data, Network.NetPeer peer)
        {
            if(Channels.TryGetValue(data.ChannelId, out var channel) && channel != JoinedChannel)
            {
                JoinedChannel = channel;
                OnChannelJoined?.Invoke(channel);
            }
            else if(JoinedChannel != null)
            {
                OnChannelLeft?.Invoke(JoinedChannel);
                JoinedChannel = null;
            }
        }

        private void VoiceCraftSocketLeaveChannel(Core.Packets.VoiceCraft.LeaveChannel data, Network.NetPeer peer)
        {
            ClearParticipants();
            if (JoinedChannel != null)
            {
                OnChannelLeft?.Invoke(JoinedChannel);
                JoinedChannel = null;
            }
        }

        private void VoiceCraftSocketServerAudio(Core.Packets.VoiceCraft.ServerAudio data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant))
            {
                participant.ProximityVolume = LinearProximity ? (float)((Math.Exp(data.Volume) - 1) / (Math.E - 1)) : data.Volume;
                participant.EchoFactor = data.EchoFactor;
                participant.Muffled = data.Muffled;
                if ((PositioningType != PositioningTypes.ClientSided || UseCustomProtocol) && DirectionalHearing)
                {
                    participant.RightVolume = (float)Math.Max(0.5 + Math.Cos(data.Rotation) * 0.5, 0.2);
                    participant.LeftVolume = (float)Math.Max(0.5 - Math.Cos(data.Rotation) * 0.5, 0.2);
                }
                participant.AddSamples(data.Audio, data.PacketCount);
            }
        }

        private void VoiceCraftSocketDenyReceived(Core.Packets.VoiceCraft.Deny data, Network.NetPeer peer)
        {
            if(State == ConnectionState.Connected)
            {
                OnDeny?.Invoke(data.Reason);
            }
        }
        #endregion

        #region MCWSS
        private void MCWSSConnected(string Username)
        {
            if (State != ConnectionState.Connected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Binded() { Name = Username });
            OnBinded?.Invoke(Username);
        }

        private async void MCWSSFailed(Exception ex)
        {
            if (State != ConnectionState.Connected) return;

            await DisconnectAsync(ex.Message);
        }

        private void MCWSSPlayerTravelled(System.Numerics.Vector3 position, string Dimension)
        {
            if (State != ConnectionState.Connected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.UpdatePosition() { Position = position });
            if(EnvironmentId != Dimension)
            {
                EnvironmentId = Dimension;
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.UpdateEnvironmentId() { EnvironmentId = Dimension });
            }
        }

        private void MCWSSDisconnected()
        {
            if (State != ConnectionState.Connected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Unbinded());
            ClearParticipants(); //Clear the entire list
            ClearChannels();
            OnUnbinded?.Invoke();
        }
        #endregion

        #region CustomClient
        private void CustomClientConnected(string name)
        {
            if (State != ConnectionState.Connected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Binded() { Name = name });
            OnBinded?.Invoke(name);
        }

        private void CustomClientDisconnected()
        {
            if (State != ConnectionState.Connected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Unbinded());
            ClearParticipants(); //Clear the entire list
            ClearChannels();
            OnUnbinded?.Invoke();
        }

        private async void CustomClientFailed(Exception ex)
        {
            if (State != ConnectionState.Connected) return;

            await DisconnectAsync(ex.Message);
        }

        private void CustomClientUpdated(System.Numerics.Vector3 position, float rotation, float caveDensity, bool isUnderwater, string dimensionId, string levelId, string serverId)
        {
            if (State != ConnectionState.Connected) return;

            var envId = dimensionId.Truncate(30, string.Empty);
            envId = string.Concat(envId, serverId.Truncate(30, string.Empty));
            envId = string.Concat(envId, levelId.Truncate(30, string.Empty));

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.FullUpdatePosition() { Position = position, Rotation = rotation, EchoFactor = caveDensity, Muffled = isUnderwater });

            if (EnvironmentId != envId)
            {
                EnvironmentId = envId ?? string.Empty;
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.UpdateEnvironmentId() { EnvironmentId = envId ?? string.Empty });
            }
        }
        #endregion

        public static async Task<string> PingAsync(string IP, int Port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var pingTime = DateTime.UtcNow;
            byte[] packetBuffer = new byte[250];
            var PacketRegistry = new Core.Packets.PacketRegistry();
            PacketRegistry.RegisterPacket((byte)Core.Packets.VoiceCraftPacketTypes.PingInfo, typeof(Core.Packets.VoiceCraft.PingInfo));

            string message;
            try
            {
                socket.Connect(IP, Port);

                var buffer = new List<byte>();
                var ping = new Core.Packets.VoiceCraft.PingInfo();
                ping.WritePacket(ref buffer);
                await socket.SendAsync(buffer.ToArray());

                if (socket.ReceiveAsync(packetBuffer).Wait(5000))
                {
                    var packet = (Core.Packets.VoiceCraft.PingInfo)PacketRegistry.GetPacketFromDataStream(packetBuffer);
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;

                    var positioningType = string.Empty;
                    switch (packet.PositioningType)
                    {
                        case PositioningTypes.ServerSided: positioningType = "Server"; break;
                        case PositioningTypes.ClientSided: positioningType = "Client"; break;
                        case PositioningTypes.Unknown: positioningType = "Hybrid"; break;
                    }

                    message = $"MOTD: {packet.MOTD}\nConnected Participants: {packet.ConnectedParticipants}\nPositioning Type: {positioningType}\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }
                else
                {
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;
                    message = $"Timed out\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }

                socket.Dispose();
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
            }

            return message;
        }

        private enum ConnectionState
        {
            Disconnected,
            Disconnecting,
            Connecting,
            Connected
        }
    }

    public static class StringExt
    {
        public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value;
        }
    }
}
