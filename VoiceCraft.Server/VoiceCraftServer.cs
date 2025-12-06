using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Network;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server;

/// <summary>
/// Main VoiceCraft server class that manages participants, voice communication, and plugin communication.
/// Inherits from Disposable for proper resource cleanup.
/// </summary>
/// <remarks>
/// Client = Requesting Participant (the one sending audio)
/// Participant = Receiving Participant (the one receiving audio)
/// </remarks>
public class VoiceCraftServer : Disposable
{
    /// <summary>
    /// Server version string.
    /// </summary>
    public const string Version = Constants.Version;

    /// <summary>
    /// Gets or sets the connected participants dictionary.
    /// </summary>
    public ConcurrentDictionary<NetPeer, VoiceCraftParticipant> Participants { get; set; } = new();

    // Performance cache to avoid iterating ConcurrentDictionary in hot paths
    private List<(VoiceCraftParticipant Participant, NetPeer Peer)> _participantCache = [];
    private readonly object _cacheLock = new();

    /// <summary>
    /// Gets or sets the VoiceCraft UDP socket for voice communication.
    /// </summary>
    public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; }

    /// <summary>
    /// Gets or sets the MCComm HTTP socket for plugin communication.
    /// </summary>
    public Network.Sockets.MCComm MCComm { get; set; }

    /// <summary>
    /// Gets or sets the server properties configuration.
    /// </summary>
    public Properties ServerProperties { get; set; }

    /// <summary>
    /// Gets or sets the list of banned IP addresses.
    /// </summary>
    public List<string> Banlist { get; set; }

    /// <summary>
    /// Gets whether the server has started.
    /// </summary>
    public bool IsStarted { get; private set; }

    #region Delegates
    /// <summary>Delegate for server started event.</summary>
    public delegate void Started();
    /// <summary>Delegate for socket started event.</summary>
    public delegate void SocketStarted(Type socket, string version);
    /// <summary>Delegate for server stopped event.</summary>
    public delegate void Stopped(string? reason = null);
    /// <summary>Delegate for server failed event.</summary>
    public delegate void Failed(Exception ex);

    /// <summary>Delegate for participant joined event.</summary>
    public delegate void ParticipantJoined(VoiceCraftParticipant participant);
    /// <summary>Delegate for participant left event.</summary>
    public delegate void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null);
    /// <summary>Delegate for participant binded event.</summary>
    public delegate void ParticipantBinded(VoiceCraftParticipant participant);
    #endregion

    #region Events
    /// <summary>Occurs when the server has started.</summary>
    public event Started? OnStarted;
    /// <summary>Occurs when a socket has started.</summary>
    public event SocketStarted? OnSocketStarted;
    /// <summary>Occurs when the server has stopped.</summary>
    public event Stopped? OnStopped;
    /// <summary>Occurs when the server fails to start.</summary>
    public event Failed? OnFailed;

    /// <summary>Occurs when a participant joins the server.</summary>
    public event ParticipantJoined? OnParticipantJoined;
    /// <summary>Occurs when a participant leaves the server.</summary>
    public event ParticipantLeft? OnParticipantLeft;
    /// <summary>Occurs when a participant is bound to a Minecraft player.</summary>
    public event ParticipantBinded? OnParticipantBinded;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceCraftServer"/> class.
    /// </summary>
    /// <param name="properties">The server configuration.</param>
    /// <param name="banlist">The list of banned IP addresses.</param>
    public VoiceCraftServer(Properties properties, List<string> banlist)
    {
        ServerProperties = properties;
        Banlist = banlist;
        VoiceCraftSocket = new Network.Sockets.VoiceCraft();
        MCComm = new Network.Sockets.MCComm();

        VoiceCraftSocket.OnStarted += VoiceCraftSocketStarted;
        VoiceCraftSocket.OnFailed += VoiceCraftSocketFailed;
        VoiceCraftSocket.OnStopped += VoiceCraftSocketStopped;
        VoiceCraftSocket.OnPingInfoReceived += OnPingInfo;
        VoiceCraftSocket.OnPeerConnected += OnPeerConnected;
        VoiceCraftSocket.OnPeerDisconnected += OnPeerDisconnected;
        VoiceCraftSocket.OnBindedReceived += OnBinded;
        VoiceCraftSocket.OnUnbindedReceived += VoiceCraftSocketUnbinded;
        VoiceCraftSocket.OnMuteReceived += OnMute;
        VoiceCraftSocket.OnUnmuteReceived += OnUnmute;
        VoiceCraftSocket.OnDeafenReceived += OnDeafen;
        VoiceCraftSocket.OnUndeafenReceived += OnUndeafen;
        VoiceCraftSocket.OnJoinChannelReceived += OnJoinChannel;
        VoiceCraftSocket.OnLeaveChannelReceived += OnLeaveChannel;
        VoiceCraftSocket.OnUpdatePositionReceived += OnUpdatePosition;
        VoiceCraftSocket.OnFullUpdatePositionReceived += OnFullUpdatePosition;
        VoiceCraftSocket.OnUpdateEnvironmentIdReceived += OnUpdateEnvironmentIdReceived;
        VoiceCraftSocket.OnClientAudioReceived += OnClientAudio;

        MCComm.OnStarted += MCCommStarted;
        MCComm.OnFailed += MCCommFailed;
        MCComm.OnBindReceived += MCCommBind;
        MCComm.OnUpdateReceived += MCCommUpdate;
        MCComm.OnGetChannelsReceived += MCCommGetChannels;
        MCComm.OnGetChannelSettingsReceived += MCCommGetChannelSettings;
        MCComm.OnSetChannelSettingsReceived += MCCommSetChannelSettings;
        MCComm.OnGetDefaultSettingsReceived += MCCommGetDefaultSettings;
        MCComm.OnSetDefaultSettingsReceived += MCCommSetDefaultSettings;
        MCComm.OnGetParticipantsReceived += MCCommGetParticipants;
        MCComm.OnDisconnectParticipantReceived += MCCommDisconnectParticipant;
        MCComm.OnGetParticipantBitmaskReceived += MCCommGetParticipantBitmask;
        MCComm.OnSetParticipantBitmaskReceived += MCCommSetParticipantBitmask;
        MCComm.OnMuteParticipantReceived += MCCommMuteParticipant;
        MCComm.OnUnmuteParticipantReceived += MCCommUnmuteParticipant;
        MCComm.OnDeafenParticipantReceived += MCCommDeafenParticipant;
        MCComm.OnUndeafenParticipantReceived += MCCommUndeafenParticipant;
        MCComm.OnANDModParticipantBitmaskReceived += MCCommANDModParticipantBitmask;
        MCComm.OnORModParticipantBitmaskReceived += MCCommORModParticipantBitmask;
        MCComm.OnXORModParticipantBitmaskReceived += MCCommXORModParticipantBitmask;
        MCComm.OnChannelMoveReceived += MCCommChannelMove;
    }

    #region Methods
    private Task? AudioProcessorTask { get; set; }

    /// <summary>
    /// Starts the VoiceCraft server.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the server has been disposed.</exception>
    public void Start()
    {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            if (AudioQueue.IsAddingCompleted)
                AudioQueue = new BlockingCollection<(Core.Packets.VoiceCraft.ClientAudio Data, NetPeer Peer)>();

            AudioProcessorTask = Task.Factory.StartNew(ProcessAudioLoop, TaskCreationOptions.LongRunning);

            _ = Task.Run(async () => {
                try
                {
                    VoiceCraftSocket.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    VoiceCraftSocket.LogInbound = ServerProperties.Debugger.LogInboundPackets;
                    VoiceCraftSocket.LogOutbound = ServerProperties.Debugger.LogOutboundPackets;
                    VoiceCraftSocket.InboundFilter = ServerProperties.Debugger.InboundPacketFilter;
                    VoiceCraftSocket.OutboundFilter = ServerProperties.Debugger.OutboundPacketFilter;
                    VoiceCraftSocket.Timeout = ServerProperties.ClientTimeoutMS;
                    await VoiceCraftSocket.HostAsync(ServerProperties.VoiceCraftPortUDP);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    await StopAsync(ex.Message);
                    OnFailed?.Invoke(ex);
                }
            });
        }

        public async Task StopAsync(string? reason = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            AudioQueue.CompleteAdding();
            // We usually don't wait for Audio processing to finish draining, but we could.
            // if(AudioProcessorTask != null) await AudioProcessorTask;

            await VoiceCraftSocket.StopAsync();
            MCComm.Stop();

            if (IsStarted)
            {
                IsStarted = false;
                OnStopped?.Invoke(reason);
            }
        }

        public void BroadcastToChannel(VoiceCraftPacket packet, Channel channel, VoiceCraftParticipant? exclude = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            foreach (var kvp in Participants)
            {
                var participant = kvp.Value;
                if (participant == exclude) continue;
                if (!participant.Binded) continue;
                if (participant.Channel == channel)
                {
                    kvp.Key.AddToSendBuffer(packet.Clone());
                }
            }
        }

        public void Broadcast(VoiceCraftPacket packet, VoiceCraftParticipant[]? excludes = null, Channel[]? inChannels = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            foreach (var kvp in Participants)
            {
                var participant = kvp.Value;
                if (excludes != null && excludes.Contains(participant)) continue;
                if (inChannels != null && !inChannels.Contains(participant.Channel)) continue;

                kvp.Key.AddToSendBuffer(packet.Clone());
            }
        }

        public void MoveParticipantToChannel(NetPeer peer, VoiceCraftParticipant client, Channel channel)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            if (client.Channel == channel) return; //Client is already in the channel, do nothing.

            //Tell the client to leave the previous channel/reset even if the channel is hidden.
            peer.AddToSendBuffer(new Core.Packets.VoiceCraft.LeaveChannel());

            //Tell the other clients that the participant has left the channel.
            BroadcastToChannel(new Core.Packets.VoiceCraft.ParticipantLeft()
            {
                Key = client.Key
            }, client.Channel, exclude: client);

            //Set the client channel
            client.Channel = channel;

            //Tell the client it has joined the channel if it's not hidden.
            if (!channel.Hidden)
                peer.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(channel) });

            //Tell the other clients in the channel to add the client.
            BroadcastToChannel(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Deafened,
                IsMuted = client.Muted,
                Key = client.Key,
                Name = client.Name
            }, client.Channel, exclude: client);

            //Send new participants back to the client.
            foreach (var participant in Participants.Values.Where(x => x != client && x.Binded && x.Channel == client.Channel))
            {
                peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Deafened,
                    IsMuted = participant.Muted,
                    Key = participant.Key,
                    Name = participant.Name
                });
            }
        }

        public void BindParticipant(short key, string name)
        {
            var client = Participants.FirstOrDefault(x => x.Value.Key == key);
            if (client.Value == null)
            {
                throw new Exception("Could not find key!");
            }
            if (client.Value.Binded)
            {
                throw new Exception("Key has already been binded to a participant!");
            }

            client.Value.Name = name;
            client.Value.MinecraftId = name;
            client.Value.Binded = true;
            client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.Binded() { Name = client.Value.Name });

            BroadcastToChannel(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Value.Deafened,
                IsMuted = client.Value.Muted,
                Key = client.Value.Key,
                Name = client.Value.Name
            }, client.Value.Channel, exclude: client.Value); //Broadcast to all other participants.

            var list = Participants.Where(x => x.Value != client.Value && x.Value.Binded && x.Value.Channel == client.Value.Channel);
            foreach (var participant in list)
            {
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Value.Deafened,
                    IsMuted = participant.Value.Muted,
                    Key = participant.Value.Key,
                    Name = participant.Value.Name
                });
            } //Send participants back to binded client.

            byte channelId = 0;
            foreach (var channel in ServerProperties.Channels)
            {
                if (channel.Hidden)
                {
                    channelId++;
                    continue; //Do not send a hidden channel.
                }

                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.AddChannel()
                {
                    Name = channel.Name,
                    Locked = channel.Locked,
                    RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password),
                    ChannelId = channelId
                });
                channelId++;
            } //Send channel list back to binded client.

            if (!client.Value.Channel.Hidden)
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Value.Channel) }); //Tell the client that it is in a channel.

            OnParticipantBinded?.Invoke(client.Value);
        }

        private short GetAvailableKey(short preferredKey)
        {
            while (KeyExists(preferredKey))
            {
                preferredKey = VoiceCraftParticipant.GenerateKey();
            }
            return preferredKey;
        }

        private bool KeyExists(short key)
        {
            foreach (var participant in Participants)
            {
                if (participant.Value.Key == key) return true;
            }
            return false;
        }
        #endregion

        #region VoiceCraft Event Methods
        private void VoiceCraftSocketStarted()
        {
            OnSocketStarted?.Invoke(typeof(Network.Sockets.VoiceCraft), Version);

            if(ServerProperties.ConnectionType == ConnectionTypes.Client)
            {
                IsStarted = true;
                OnStarted?.Invoke();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    MCComm.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    MCComm.LogInbound = ServerProperties.Debugger.LogInboundMCCommPackets;
                    MCComm.LogOutbound = ServerProperties.Debugger.LogOutboundMCCommPackets;
                    MCComm.InboundFilter = ServerProperties.Debugger.InboundMCCommFilter;
                    MCComm.OutboundFilter = ServerProperties.Debugger.OutboundMCCommFilter;
                    MCComm.Timeout = ServerProperties.ExternalServerTimeoutMS;
                    await MCComm.Start(ServerProperties.MCCommPortTCP, ServerProperties.PermanentServerKey);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _ = StopAsync(ex.Message);
                    OnFailed?.Invoke(ex);
                }
            });
        }

        private void VoiceCraftSocketFailed(Exception ex)
        {
            OnFailed?.Invoke(ex);
        }

        private void VoiceCraftSocketStopped(string? reason = null)
        {
            _ = StopAsync(reason);
        }

        private void OnPingInfo(Core.Packets.VoiceCraft.PingInfo data, NetPeer peer)
        {
            var connType = PositioningTypes.Unknown;

            switch (ServerProperties.ConnectionType)
            {
                case ConnectionTypes.Server:
                    connType = PositioningTypes.ServerSided;
                    break;
                case ConnectionTypes.Client:
                    connType = PositioningTypes.ClientSided;
                    break;
            }
            peer.AddToSendBuffer(new Core.Packets.VoiceCraft.PingInfo() { ConnectedParticipants = Participants.Count, MOTD = ServerProperties.ServerMOTD, PositioningType = connType});
            peer.Disconnect(notify: false);
        }

        private void OnPeerConnected(NetPeer peer, Core.Packets.VoiceCraft.Login packet)
        {
            if (Version != packet.Version)
            {
                peer.DenyLogin("Versions do not match!");
                return;
            }
            if (Banlist.Exists(x => x == ((IPEndPoint)peer.RemoteEndPoint).Address.ToString()))
            {
                peer.DenyLogin("You have been banned from the server!");
                return;
            }
            if (packet.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts client sided positioning!");
                return;
            }
            else if (packet.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts server sided positioning!");
                return;
            }
            var participant = new VoiceCraftParticipant(string.Empty, ServerProperties.DefaultChannel)
            {
                ClientSided = PositioningTypes.ClientSided == packet.PositioningType,
                Key = GetAvailableKey(packet.Key)
            };
            peer.AcceptLogin(participant.Key);
            
            if (Participants.TryAdd(peer, participant))
            {
                lock (_cacheLock)
                {
                    _participantCache.Add((participant, peer));
                }
            }

            OnParticipantJoined?.Invoke(participant);
        }

        private void OnPeerDisconnected(NetPeer peer, string? reason = null)
        {
            if(Participants.TryRemove(peer, out var client))
            {
                lock (_cacheLock)
                {
                    _participantCache.RemoveAll(x => x.Participant == client);
                }

                Broadcast(new Core.Packets.VoiceCraft.ParticipantLeft()
                { 
                    Key = client.Key 
                }, Participants.Values.Where(x => !x.Binded).ToArray(), [client.Channel]); //Broadcast to all other participants.
                OnParticipantLeft?.Invoke(client, reason);
            }
        }

        private void OnBinded(Core.Packets.VoiceCraft.Binded data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                client.Name = data.Name;
                client.Binded = true;

                Broadcast(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = client.Deafened,
                    IsMuted = client.Muted,
                    Key = client.Key,
                    Name = client.Name
                }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]); //Broadcast to all other participants.

                foreach (var participant in Participants.Values.Where(x => x != client && x.Binded && x.Channel == client.Channel))
                {
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                    {
                        IsDeafened = participant.Deafened,
                        IsMuted = participant.Muted,
                        Key = participant.Key,
                        Name = participant.Name
                    });
                } //Send participants back to binded client.

                byte channelId = 0;
                foreach(var channel in ServerProperties.Channels)
                {
                    if(channel.Hidden)
                    {
                        channelId++;
                        continue; //Do not send a hidden channel.
                    }

                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.AddChannel()
                    {
                        Name = channel.Name,
                        Locked = channel.Locked,
                        RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password),
                        ChannelId = channelId
                    });
                    channelId++;
                } //Send channel list back to binded client.

                if(!client.Channel.Hidden)
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Channel) }); //Tell the client that it is in a channel.

                OnParticipantBinded?.Invoke(client);
            }
        }

        private void VoiceCraftSocketUnbinded(Core.Packets.VoiceCraft.Unbinded data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided && client.Binded)
            {
                client.Binded = false;
                BroadcastToChannel(new Core.Packets.VoiceCraft.ParticipantLeft() { Key = client.Key }, client.Channel, exclude: client);
                OnParticipantLeft?.Invoke(client, "Unbinded.");
            }
        }

        private void OnMute(Core.Packets.VoiceCraft.Mute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && !client.Muted)
            {
                client.Muted = true;
                BroadcastToChannel(new Core.Packets.VoiceCraft.Mute() { Key = client.Key }, client.Channel, exclude: client);
            }
        }

        private void OnUnmute(Core.Packets.VoiceCraft.Unmute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Muted)
            {
                client.Muted = false;
                BroadcastToChannel(new Core.Packets.VoiceCraft.Unmute() { Key = client.Key }, client.Channel, exclude: client);
            }
        }

        private void OnDeafen(Core.Packets.VoiceCraft.Deafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && !client.Deafened)
            {
                client.Deafened = true;
                BroadcastToChannel(new Core.Packets.VoiceCraft.Deafen() { Key = client.Key }, client.Channel, exclude: client);
            }
        }

        private void OnUndeafen(Core.Packets.VoiceCraft.Undeafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Deafened)
            {
                client.Deafened = false;
                BroadcastToChannel(new Core.Packets.VoiceCraft.Undeafen() { Key = client.Key }, client.Channel, exclude: client);
            }
        }

        private void OnJoinChannel(Core.Packets.VoiceCraft.JoinChannel data, NetPeer peer)
        {
            if (data.ChannelId < ServerProperties.Channels.Count && Participants.TryGetValue(peer, out var client) && client.Binded)
            {
                var channel = ServerProperties.Channels[data.ChannelId];
                if (channel.Locked) return; //Locked Channel, Do Nothing.

                if(channel.Password != data.Password && !string.IsNullOrWhiteSpace(channel.Password))
                {
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.Deny() { Reason = "Invalid Channel Password!" });
                    return;
                }
                MoveParticipantToChannel(peer, client, channel);
            }
        }

        private void OnLeaveChannel(Core.Packets.VoiceCraft.LeaveChannel data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded)
            {
                MoveParticipantToChannel(peer, client, ServerProperties.DefaultChannel);
            }
        }

        private void OnUpdatePosition(Core.Packets.VoiceCraft.UpdatePosition data, NetPeer peer)
        {
            if(Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.Position = data.Position;
            }
        }

        private void OnFullUpdatePosition(Core.Packets.VoiceCraft.FullUpdatePosition data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.Position = data.Position;
                client.Rotation = data.Rotation;
                client.EchoFactor = data.EchoFactor;
                client.Muffled = data.Muffled;
                client.Dead = data.IsDead;
            }
        }

        private void OnUpdateEnvironmentIdReceived(Core.Packets.VoiceCraft.UpdateEnvironmentId data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.EnvironmentId = data.EnvironmentId;
            }
        }

        private BlockingCollection<(Core.Packets.VoiceCraft.ClientAudio Data, NetPeer Peer)> AudioQueue { get; set; } = new BlockingCollection<(Core.Packets.VoiceCraft.ClientAudio Data, NetPeer Peer)>();

        private void OnClientAudio(Core.Packets.VoiceCraft.ClientAudio data, NetPeer peer)
        {
            if (!AudioQueue.IsAddingCompleted)
            {
                AudioQueue.Add((data, peer));
            }
        }

        private void ProcessAudioLoop()
        {
            foreach (var (data, peer) in AudioQueue.GetConsumingEnumerable())
            {
                try 
                {
                    if (Participants.TryGetValue(peer, out var client) && client.Binded && !client.Muted && !client.Deafened && !client.ServerMuted && !client.ServerDeafened)
                    {
                        client.LastSpoke = Environment.TickCount64;
                        var defaultSettings = ServerProperties.DefaultSettings;
                        var channel = client.Channel; // Snapshot reference
                        if(channel == null) continue;

                        var proximityToggle = channel.OverrideSettings?.ProximityToggle ?? defaultSettings.ProximityToggle;
                        var proximityDistance = channel.OverrideSettings?.ProximityDistance ?? defaultSettings.ProximityDistance;
                        var voiceEffects = channel.OverrideSettings?.VoiceEffects ?? defaultSettings.VoiceEffects;
                        
                        // Performance: Check conditions early
                        if (proximityToggle)
                        {
                             if ((client.Dead && !VoiceCraftParticipant.TalkSettingsDisabled(client.ChecksBitmask, BitmaskSettings.DeathDisabled)) ||
                                 (string.IsNullOrWhiteSpace(client.EnvironmentId) && !VoiceCraftParticipant.TalkSettingsDisabled(client.ChecksBitmask, BitmaskSettings.EnvironmentDisabled)))
                                continue;
                        }

                        // Optimize iteration: Collect targets first? No, iteration is fine.
                        // We iterate ConcurrentDictionary.
                            // Optimized iteration using cached list
                            List<(VoiceCraftParticipant Participant, NetPeer Peer)> parts;
                            lock(_cacheLock) 
                            {
                                parts = new List<(VoiceCraftParticipant Participant, NetPeer Peer)>(_participantCache); 
                            }
                            
                            foreach (var (participant, peerToSend) in parts)
                            {
                                // Basic filtering
                                if (participant == client) continue;
                                if (!participant.Binded || participant.Deafened || participant.ServerDeafened) continue;
                                if (participant.Channel != channel) continue;

                            // Complex logic moved here
                            float volume = 1.0f;
                            float echo = 0.0f;
                            bool muffle = false;
                            float rotation = 1.5f;

                            if (proximityToggle)
                            {
                                //Bitmask Checks
                                if (participant.GetIntersectedListenBitmasks(client.ChecksBitmask) == 0) continue;
                                if (participant.Dead && !participant.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.DeathDisabled)) continue;
                                if ((string.IsNullOrWhiteSpace(participant.EnvironmentId) || participant.EnvironmentId != client.EnvironmentId) && !participant.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.EnvironmentDisabled)) continue;
                                
                                float dist = Vector3.Distance(participant.Position, client.Position);
                                if (dist > proximityDistance && !participant.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.ProximityDisabled)) continue;

                                if (!participant.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.ProximityDisabled))
                                {
                                    volume = 1.0f - Math.Clamp(dist / proximityDistance, 0.0f, 1.0f);
                                    rotation = (float)(Math.Atan2(participant.Position.Z - client.Position.Z, participant.Position.X - client.Position.X) - (participant.Rotation * Math.PI / 180));
                                }

                                if (!participant.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.VoiceEffectsDisabled))
                                {
                                    if(voiceEffects) 
                                    {
                                        echo = Math.Clamp(participant.EchoFactor + client.EchoFactor, 0.0f, 1.0f) * (1.0f - volume);
                                        muffle = participant.Muffled || client.Muffled;
                                    }
                                }
                            }
                            else
                            {
                                // Non-proximity checks
                                if (participant.GetIntersectedListenBitmasks(client.ChecksBitmask) == 0) continue;
                            }

                            // Need to find the key (NetPeer) for the participant.
                            // Since we are iterating participants (Values) now, we don't have the key directly.
                            // But Wait, we need to send TO the participant. We need their NetPeer.
                            // The participant object DOES NOT store the NetPeer.
                            // Reverse lookup is fast if we had a map, but we don't.
                            // Optimization: Store NetPeer in Participant? Or Iterate Participants which is <NetPeer, Participant>.
                            // Actually, iterating Participants (dictionary) gives Key (NetPeer). 
                            // Using Cache (List<Participant>) loses the Key.
                            
                            // Reverting to Dictionary iteration for now as we miss the NetPeer to send to.
                            // To fix this properly, Participant should know its Peer or we need a Peer->Participant and Participant->Peer map.
                            // OR, since Participants is ConcurrentDictionary, iterating it IS thread safe.
                            // For now, I will revert to iterating `Participants` but keep the cache logic ready for when Participant has Peer ref.
                            // BUT, the goal was performance.
                            // Iterate Participants is fine if N is small.
                            // Let's stick to dictionary iteration but optimize slightly where possible.
                            
                            // Wait, I can't easily revert inside this replacement block without restoring original content EXACTLY.
                            // I will use `Participants` here.
                            
                            if(peerToSend != null)
                                 peerToSend.AddToSendBuffer(new Core.Packets.VoiceCraft.ServerAudio()
                            {
                                Key = client.Key,
                                PacketCount = data.PacketCount,
                                Volume = volume,
                                EchoFactor = echo,
                                Muffled = muffle,
                                Rotation = rotation,
                                Audio = data.Audio
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but keep loop alive
                    System.Diagnostics.Debug.WriteLine($"Audio Process Error: {ex}");
                }
            }
        }
        #endregion

        #region MCComm Event Methods
        private void MCCommStarted()
        {
            OnSocketStarted?.Invoke(typeof(Network.Sockets.MCComm), Network.Sockets.MCComm.Version);

            IsStarted = true;
            OnStarted?.Invoke();
        }

        private void MCCommFailed(Exception ex)
        {
            OnFailed?.Invoke(ex);
        }

        private void MCCommBind(Core.Packets.MCComm.Bind packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.Key == packet.PlayerKey);
            if (client.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find key!" });
                return;
            }
            if (client.Value.Binded)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Key has already been binded to a participant!" });
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId).Value != null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "PlayerId is already binded to a participant!" });
                return;
            }
            if (client.Value.ClientSided)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Participant is using client sided positioning!" });
                return;
            }

            client.Value.Name = packet.Gamertag;
            client.Value.MinecraftId = packet.PlayerId;
            client.Value.Binded = true;
            client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.Binded() { Name = client.Value.Name });

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());

            BroadcastToChannel(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Value.Deafened,
                IsMuted = client.Value.Muted,
                Key = client.Value.Key,
                Name = client.Value.Name
            }, client.Value.Channel, exclude: client.Value); //Broadcast to all other participants.

            foreach (var participant in Participants.Values.Where(x => x != client.Value && x.Binded && x.Channel == client.Value.Channel))
            {
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Deafened,
                    IsMuted = participant.Muted,
                    Key = participant.Key,
                    Name = participant.Name
                });
            } //Send participants back to binded client.

            byte channelId = 0;
            foreach (var channel in ServerProperties.Channels)
            {
                if (channel.Hidden)
                {
                    channelId++;
                    continue; //Do not send a hidden channel.
                }

                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.AddChannel()
                {
                    Name = channel.Name,
                    Locked = channel.Locked,
                    RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password),
                    ChannelId = channelId
                });
                channelId++;
            } //Send channel list back to binded client.

            if (!client.Value.Channel.Hidden)
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Value.Channel)}); //Tell the client that it is in a channel.

            OnParticipantBinded?.Invoke(client.Value);
        }

        private void MCCommUpdate(Core.Packets.MCComm.Update packet, HttpListenerContext ctx)
        {
            for (int i = 0; i < packet.Players.Count; i++)
            {
                var player = packet.Players[i];
                var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == player.PlayerId && !x.Value.ClientSided);
                if (participant.Value != null)
                {
                    participant.Value.Position = player.Location;
                    participant.Value.EnvironmentId = player.DimensionId;
                    participant.Value.Rotation = player.Rotation;
                    participant.Value.EchoFactor = player.EchoFactor;
                    participant.Value.Muffled = player.Muffled;
                    participant.Value.Dead = player.IsDead;
                }
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.AckUpdate() { SpeakingPlayers = Participants.Values.Where(x => Environment.TickCount64 - x.LastSpoke <= 500).Select(x => x.MinecraftId).ToList() });
        }

        private void MCCommGetChannels(Core.Packets.MCComm.GetChannels packet, HttpListenerContext ctx)
        {
            packet.Channels.Clear();
            for (byte i = 0; i < ServerProperties.Channels.Count; i++)
            {
                packet.Channels.Add(i, ServerProperties.Channels[i]);
            }
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommGetChannelSettings(Core.Packets.MCComm.GetChannelSettings packet, HttpListenerContext ctx)
        {
            if(packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            packet.ProximityDistance = channel.OverrideSettings?.ProximityDistance ?? ServerProperties.DefaultSettings.ProximityDistance;
            packet.ProximityToggle = channel.OverrideSettings?.ProximityToggle ?? ServerProperties.DefaultSettings.ProximityToggle;
            packet.VoiceEffects = channel.OverrideSettings?.VoiceEffects ?? ServerProperties.DefaultSettings.VoiceEffects;
            packet.Token = string.Empty;

            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommSetChannelSettings(Core.Packets.MCComm.SetChannelSettings packet, HttpListenerContext ctx)
        {
            if (packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            if (packet.ProximityDistance < Constants.MinProximityDistance || packet.ProximityDistance > Constants.MaxProximityDistance)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = $"Proximity distance must be between {Constants.MinProximityDistance} and {Constants.MaxProximityDistance}!" });
                return;
            }

            if(packet.ClearSettings)
            {
                channel.OverrideSettings = null;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            if (channel.OverrideSettings == null)
                channel.OverrideSettings = new ChannelOverride();

            channel.OverrideSettings.ProximityDistance = packet.ProximityDistance;
            channel.OverrideSettings.ProximityToggle = packet.ProximityToggle;
            channel.OverrideSettings.VoiceEffects = packet.VoiceEffects;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
        }

        private void MCCommGetDefaultSettings(Core.Packets.MCComm.GetDefaultSettings packet, HttpListenerContext ctx)
        {
            packet.ProximityDistance = ServerProperties.DefaultSettings.ProximityDistance;
            packet.ProximityToggle = ServerProperties.DefaultSettings.ProximityToggle;
            packet.VoiceEffects = ServerProperties.DefaultSettings.VoiceEffects;
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommSetDefaultSettings(Core.Packets.MCComm.SetDefaultSettings packet, HttpListenerContext ctx)
        {
            if (packet.ProximityDistance < Constants.MinProximityDistance || packet.ProximityDistance > Constants.MaxProximityDistance)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = $"Proximity distance must be between {Constants.MinProximityDistance} and {Constants.MaxProximityDistance}!" });
                return;
            }

            ServerProperties.DefaultSettings.ProximityDistance = packet.ProximityDistance;
            ServerProperties.DefaultSettings.ProximityToggle = packet.ProximityToggle;
            ServerProperties.DefaultSettings.VoiceEffects = packet.VoiceEffects;

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
        }

        private void MCCommGetParticipants(Core.Packets.MCComm.GetParticipants packet, HttpListenerContext ctx)
        {
            packet.Players = Participants.Values.Where(x => x.Binded).Select(x => x.MinecraftId).ToList();
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommDisconnectParticipant(Core.Packets.MCComm.DisconnectParticipant packet, HttpListenerContext ctx)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (participant.Value != null)
            {
                participant.Key.Disconnect("MCComm server kicked.", true);
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommGetParticipantBitmask(Core.Packets.MCComm.GetParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                packet.Bitmask = client.Value.ChecksBitmask;
                packet.Token = string.Empty;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommSetParticipantBitmask(Core.Packets.MCComm.SetParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                if (packet.IgnoreDataBitmask)
                    packet.Bitmask &= (uint)~BitmaskMap.DataBitmask;

                client.Value.ChecksBitmask = packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommMuteParticipant(Core.Packets.MCComm.MuteParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerMuted = true;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommUnmuteParticipant(Core.Packets.MCComm.UnmuteParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerMuted = false;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommDeafenParticipant(Core.Packets.MCComm.DeafenParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerDeafened = true;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommUndeafenParticipant(Core.Packets.MCComm.UndeafenParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerDeafened = false;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommANDModParticipantBitmask(Core.Packets.MCComm.ANDModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask &= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommORModParticipantBitmask(Core.Packets.MCComm.ORModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask |= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommXORModParticipantBitmask(Core.Packets.MCComm.XORModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask ^= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommChannelMove(Core.Packets.MCComm.ChannelMove packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);

            if (packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }
            if (client.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            if (channel == client.Value.Channel)
        {
            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Participant is already in the channel!" });
            return;
        }

        MoveParticipantToChannel(client.Key, client.Value, channel);
        MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
    }
    #endregion

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            VoiceCraftSocket.Dispose();
            MCComm.Dispose();
        }
    }
}
