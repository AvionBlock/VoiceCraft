using System.Collections.Concurrent;
using System.Net;
using System.Text;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Network.Sockets
{
    public class MCComm : Disposable
    {
        #region Variables
        // Keep MCComm protocol version aligned with core version to avoid login denials.
        public const string Version = Core.Constants.Version;
        private HttpListener WebServer = new HttpListener();
        public string LoginKey { get; private set; } = string.Empty;
        public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
        public ConcurrentDictionary<string, long> Sessions { get; set; } = new ConcurrentDictionary<string, long>();
        public int Timeout { get; set; } = 8000;
        private Task? ActivityChecker { get; set; }
        #endregion

        #region Debug Variables
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<MCCommPacketTypes> InboundFilter { get; set; } = new List<MCCommPacketTypes>();
        public List<MCCommPacketTypes> OutboundFilter { get; set; } = new List<MCCommPacketTypes>();
        #endregion

        #region Delegates
        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void ServerConnected(string token, string address);
        public delegate void ServerDisconnected(string reason, string token);
        public delegate void PacketData<T>(T packet, HttpListenerContext ctx);

        public delegate void InboundPacket(MCCommPacket packet);
        public delegate void OutboundPacket(MCCommPacket packet);
        public delegate void ExceptionError(Exception error);
        public delegate void Failed(Exception ex);
        #endregion

        #region Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event ServerConnected? OnServerConnected;
        public event ServerDisconnected? OnServerDisconnected;

        public event PacketData<Login>? OnLoginReceived;
        public event PacketData<Logout>? OnLogoutReceived;
        public event PacketData<Accept>? OnAcceptReceived;
        public event PacketData<Deny>? OnDenyReceived;
        public event PacketData<Bind>? OnBindReceived;
        public event PacketData<Update>? OnUpdateReceived;
        public event PacketData<AckUpdate>? OnAckUpdateReceived;
        public event PacketData<GetChannels>? OnGetChannelsReceived;
        public event PacketData<GetChannelSettings>? OnGetChannelSettingsReceived;
        public event PacketData<SetChannelSettings>? OnSetChannelSettingsReceived;
        public event PacketData<GetDefaultSettings>? OnGetDefaultSettingsReceived;
        public event PacketData<SetDefaultSettings>? OnSetDefaultSettingsReceived;
        public event PacketData<GetParticipants>? OnGetParticipantsReceived;
        public event PacketData<DisconnectParticipant>? OnDisconnectParticipantReceived;
        public event PacketData<GetParticipantBitmask>? OnGetParticipantBitmaskReceived;
        public event PacketData<SetParticipantBitmask>? OnSetParticipantBitmaskReceived;
        public event PacketData<MuteParticipant>? OnMuteParticipantReceived;
        public event PacketData<UnmuteParticipant>? OnUnmuteParticipantReceived;
        public event PacketData<DeafenParticipant>? OnDeafenParticipantReceived;
        public event PacketData<UndeafenParticipant>? OnUndeafenParticipantReceived;
        public event PacketData<ANDModParticipantBitmask>? OnANDModParticipantBitmaskReceived;
        public event PacketData<ORModParticipantBitmask>? OnORModParticipantBitmaskReceived;
        public event PacketData<XORModParticipantBitmask>? OnXORModParticipantBitmaskReceived;
        public event PacketData<ChannelMove>? OnChannelMoveReceived;

        public event InboundPacket? OnInboundPacket;
        public event OutboundPacket? OnOutboundPacket;
        public event ExceptionError? OnExceptionError;
        public event Failed? OnFailed;
        #endregion

        public MCComm()
        {
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Login, typeof(Login));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Logout, typeof(Logout));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Accept, typeof(Accept));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Deny, typeof(Deny));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Bind, typeof(Bind));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Update, typeof(Update));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.AckUpdate, typeof(AckUpdate));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetChannels, typeof(GetChannels));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetChannelSettings, typeof(GetChannelSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetChannelSettings, typeof(SetChannelSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetDefaultSettings, typeof(GetDefaultSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetDefaultSettings, typeof(SetDefaultSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetParticipants, typeof(GetParticipants));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.DisconnectParticipant, typeof(DisconnectParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetParticipantBitmask, typeof(GetParticipantBitmask));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.SetParticipantBitmask, typeof(SetParticipantBitmask));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.MuteParticipant, typeof(MuteParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.UnmuteParticipant, typeof(UnmuteParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.DeafenParticipant, typeof(DeafenParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.UndeafenParticipant, typeof(UndeafenParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ANDModParticipantBitmask, typeof(ANDModParticipantBitmask));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ORModParticipantBitmask, typeof(ORModParticipantBitmask));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.XORModParticipantBitmask, typeof(XORModParticipantBitmask));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ChannelMove, typeof(ChannelMove));
        }

        public async Task Start(ushort Port, string LoginKey)
        {
            try
            {
                this.LoginKey = LoginKey;
                WebServer.Prefixes.Add($"http://*:{Port}/");
                WebServer.Start();
                OnLoginReceived += LoginReceived;
                OnLogoutReceived += LogoutReceived;
                ActivityChecker = Task.Run(ActivityCheck);
                OnStarted?.Invoke();
                await ListenAsync();
            }
            catch (Exception ex)
            {
                OnFailed?.Invoke(ex);
            }
        }

        public void Stop()
        {
            if (WebServer.IsListening)
            {
                WebServer.Stop();
                OnLoginReceived -= LoginReceived;
                OnLogoutReceived -= LogoutReceived;
                Sessions.Clear();
                ActivityChecker = null;
                OnStopped?.Invoke();
            }
        }

        public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, MCCommPacket packet)
        {
            if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                OnOutboundPacket?.Invoke(packet);

            var content = Encoding.UTF8.GetBytes(packet.SerializePacket());
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "text/plain";
            ctx.Response.OutputStream.Write(content, 0, content.Length);
            ctx.Response.OutputStream.Close();
        }

        private async Task ListenAsync()
        {
            while (WebServer.IsListening)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await WebServer.GetContextAsync();

                    if (ctx.Request.HttpMethod == HttpMethod.Post.Method)
                    {
                        var content = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                        var packet = PacketRegistry.GetPacketFromJsonString(content);

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                            OnInboundPacket?.Invoke(packet);

                        HandlePacket(packet, ctx);
                    }
                }
                catch (HttpListenerException)
                {
                    return; //Done with the socket.
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (ctx != null)
                        SendResponse(ctx, HttpStatusCode.BadRequest, new Deny() { Reason = "Invalid Data!" });
                }
            }
        }

        private void HandlePacket(MCCommPacket packet, HttpListenerContext ctx)
        {
            try
            {
                if (packet.PacketId != (byte)MCCommPacketTypes.Login)
                {
                    if (Sessions.TryGetValue(packet.Token, out var session))
                        Sessions.TryUpdate(packet.Token, Environment.TickCount64, session);
                    else
                    {
                        SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Token!" });
                        return;
                    }
                }

                switch ((MCCommPacketTypes)packet.PacketId)
                {
                    case MCCommPacketTypes.Login: OnLoginReceived?.Invoke((Login)packet, ctx); break;
                    case MCCommPacketTypes.Logout: OnLogoutReceived?.Invoke((Logout)packet, ctx); break;
                    case MCCommPacketTypes.Accept: OnAcceptReceived?.Invoke((Accept)packet, ctx); break;
                    case MCCommPacketTypes.Deny: OnDenyReceived?.Invoke((Deny)packet, ctx); break;
                    case MCCommPacketTypes.Bind: OnBindReceived?.Invoke((Bind)packet, ctx); break;
                    case MCCommPacketTypes.Update: OnUpdateReceived?.Invoke((Update)packet, ctx); break;
                    case MCCommPacketTypes.GetChannels: OnGetChannelsReceived?.Invoke((GetChannels)packet, ctx); break;
                    case MCCommPacketTypes.AckUpdate: OnAckUpdateReceived?.Invoke((AckUpdate)packet, ctx); break;
                    case MCCommPacketTypes.GetChannelSettings: OnGetChannelSettingsReceived?.Invoke((GetChannelSettings)packet, ctx); break;
                    case MCCommPacketTypes.SetChannelSettings: OnSetChannelSettingsReceived?.Invoke((SetChannelSettings)packet, ctx); break;
                    case MCCommPacketTypes.GetDefaultSettings: OnGetDefaultSettingsReceived?.Invoke((GetDefaultSettings)packet, ctx); break;
                    case MCCommPacketTypes.SetDefaultSettings: OnSetDefaultSettingsReceived?.Invoke((SetDefaultSettings)packet, ctx); break;
                    case MCCommPacketTypes.GetParticipants: OnGetParticipantsReceived?.Invoke((GetParticipants)packet, ctx); break;
                    case MCCommPacketTypes.DisconnectParticipant: OnDisconnectParticipantReceived?.Invoke((DisconnectParticipant)packet, ctx); break;
                    case MCCommPacketTypes.GetParticipantBitmask: OnGetParticipantBitmaskReceived?.Invoke((GetParticipantBitmask)packet, ctx); break;
                    case MCCommPacketTypes.SetParticipantBitmask: OnSetParticipantBitmaskReceived?.Invoke((SetParticipantBitmask)packet, ctx); break;
                    case MCCommPacketTypes.MuteParticipant: OnMuteParticipantReceived?.Invoke((MuteParticipant)packet, ctx); break;
                    case MCCommPacketTypes.UnmuteParticipant: OnUnmuteParticipantReceived?.Invoke((UnmuteParticipant)packet, ctx); break;
                    case MCCommPacketTypes.DeafenParticipant: OnDeafenParticipantReceived?.Invoke((DeafenParticipant)packet, ctx); break;
                    case MCCommPacketTypes.UndeafenParticipant: OnUndeafenParticipantReceived?.Invoke((UndeafenParticipant)packet, ctx); break;
                    case MCCommPacketTypes.ANDModParticipantBitmask: OnANDModParticipantBitmaskReceived?.Invoke((ANDModParticipantBitmask)packet, ctx); break;
                    case MCCommPacketTypes.ORModParticipantBitmask: OnORModParticipantBitmaskReceived?.Invoke((ORModParticipantBitmask)packet, ctx); break;
                    case MCCommPacketTypes.XORModParticipantBitmask: OnXORModParticipantBitmaskReceived?.Invoke((XORModParticipantBitmask)packet, ctx); break;
                    case MCCommPacketTypes.ChannelMove: OnChannelMoveReceived?.Invoke((ChannelMove)packet, ctx); break;
                }
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);

                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny() { Reason = "Invalid Data!" });
            }
        }

        private void LoginReceived(Login packet, HttpListenerContext ctx)
        {
            if (packet.LoginKey != LoginKey)
            {
                SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Login Key!" });
                return;
            }
            if (packet.Version != Version)
            {
                SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Version!" });
                return;
            }
            var token = Guid.NewGuid().ToString();
            Sessions.TryAdd(token, Environment.TickCount64);
            SendResponse(ctx, HttpStatusCode.OK, new Accept() { Token = token });
            OnServerConnected?.Invoke(token, ctx.Request.RemoteEndPoint.Address.ToString());
        }

        private void LogoutReceived(Logout packet, HttpListenerContext ctx)
        {
            if (Sessions.TryRemove(packet.Token, out var session))
            {
                SendResponse(ctx, HttpStatusCode.OK, new Accept());
                OnServerDisconnected?.Invoke("Server disconnected", packet.Token);
                return;
            }
            SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Token!" });
        }

        private async Task ActivityCheck()
        {
            while (WebServer.IsListening)
            {
                foreach (var session in Sessions)
                {
                    if (Environment.TickCount64 - session.Value > Timeout && Sessions.TryRemove(session))
                    {
                        var timeoutDiff = (int)(Environment.TickCount64 - session.Value);
                        OnServerDisconnected?.Invoke($"Server timed out, Timeout: {timeoutDiff}", session.Key);
                    }
                }

                await Task.Delay(1); //1ms to not destroy the cpu.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (WebServer.IsListening)
                    Stop();
                WebServer.Close();
            }
        }
    }
}
