using System.Collections.Concurrent;
using System.Net;
using System.Text;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Network.Sockets;

/// <summary>
/// Minecraft Communication server using HTTP for server-side plugin communication.
/// Handles authentication, session management, and player updates.
/// </summary>
public class MCComm : Disposable
{
    #region Constants and Properties
    /// <summary>
    /// Protocol version - matches Core.Constants.Version.
    /// </summary>
    public const string Version = Core.Constants.Version;
    
    /// <summary>
    /// Maximum request body size in bytes (1MB).
    /// </summary>
    private const int MaxRequestSize = 1024 * 1024;

    private HttpListener _webServer = new();
    
    /// <summary>
    /// Gets the login key required for authentication.
    /// </summary>
    public string LoginKey { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the packet registry for serialization.
    /// </summary>
    public PacketRegistry PacketRegistry { get; } = new();
    
    /// <summary>
    /// Gets the active sessions by token.
    /// </summary>
    public ConcurrentDictionary<string, long> Sessions { get; } = new();
    
    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    public int Timeout { get; set; } = 8000;
    
    private Task? _activityChecker;
    private CancellationTokenSource? _cts;
    #endregion

    #region Debug Properties
    /// <summary>Gets or sets whether to log exceptions.</summary>
    public bool LogExceptions { get; set; }
    
    /// <summary>Gets or sets whether to log inbound packets.</summary>
    public bool LogInbound { get; set; }
    
    /// <summary>Gets or sets whether to log outbound packets.</summary>
    public bool LogOutbound { get; set; }
    
    /// <summary>Gets or sets the filter for inbound packet logging.</summary>
    public List<MCCommPacketTypes> InboundFilter { get; set; } = [];
    
    /// <summary>Gets or sets the filter for outbound packet logging.</summary>
    public List<MCCommPacketTypes> OutboundFilter { get; set; } = [];
    #endregion

    #region Delegates
    public delegate void StartedHandler();
    public delegate void StoppedHandler(string? reason = null);
    public delegate void ServerConnectedHandler(string token, string address);
    public delegate void ServerDisconnectedHandler(string reason, string token);
    public delegate void PacketDataHandler<T>(T packet, HttpListenerContext ctx);
    public delegate void InboundPacketHandler(MCCommPacket packet);
    public delegate void OutboundPacketHandler(MCCommPacket packet);
    public delegate void ExceptionErrorHandler(Exception error);
    public delegate void FailedHandler(Exception ex);
    #endregion

    #region Events
    /// <summary>Raised when the server starts.</summary>
    public event StartedHandler? OnStarted;
    
    /// <summary>Raised when the server stops.</summary>
    public event StoppedHandler? OnStopped;
    
    /// <summary>Raised when a Minecraft server connects.</summary>
    public event ServerConnectedHandler? OnServerConnected;
    
    /// <summary>Raised when a Minecraft server disconnects.</summary>
    public event ServerDisconnectedHandler? OnServerDisconnected;

    public event PacketDataHandler<Login>? OnLoginReceived;
    public event PacketDataHandler<Logout>? OnLogoutReceived;
    public event PacketDataHandler<Accept>? OnAcceptReceived;
    public event PacketDataHandler<Deny>? OnDenyReceived;
    public event PacketDataHandler<Bind>? OnBindReceived;
    public event PacketDataHandler<Update>? OnUpdateReceived;
    public event PacketDataHandler<AckUpdate>? OnAckUpdateReceived;
    public event PacketDataHandler<GetChannels>? OnGetChannelsReceived;
    public event PacketDataHandler<GetChannelSettings>? OnGetChannelSettingsReceived;
    public event PacketDataHandler<SetChannelSettings>? OnSetChannelSettingsReceived;
    public event PacketDataHandler<GetDefaultSettings>? OnGetDefaultSettingsReceived;
    public event PacketDataHandler<SetDefaultSettings>? OnSetDefaultSettingsReceived;
    public event PacketDataHandler<GetParticipants>? OnGetParticipantsReceived;
    public event PacketDataHandler<DisconnectParticipant>? OnDisconnectParticipantReceived;
    public event PacketDataHandler<GetParticipantBitmask>? OnGetParticipantBitmaskReceived;
    public event PacketDataHandler<SetParticipantBitmask>? OnSetParticipantBitmaskReceived;
    public event PacketDataHandler<MuteParticipant>? OnMuteParticipantReceived;
    public event PacketDataHandler<UnmuteParticipant>? OnUnmuteParticipantReceived;
    public event PacketDataHandler<DeafenParticipant>? OnDeafenParticipantReceived;
    public event PacketDataHandler<UndeafenParticipant>? OnUndeafenParticipantReceived;
    public event PacketDataHandler<ANDModParticipantBitmask>? OnANDModParticipantBitmaskReceived;
    public event PacketDataHandler<ORModParticipantBitmask>? OnORModParticipantBitmaskReceived;
    public event PacketDataHandler<XORModParticipantBitmask>? OnXORModParticipantBitmaskReceived;
    public event PacketDataHandler<ChannelMove>? OnChannelMoveReceived;

    /// <summary>Raised when a packet is received (debug).</summary>
    public event InboundPacketHandler? OnInboundPacket;
    
    /// <summary>Raised when a packet is sent (debug).</summary>
    public event OutboundPacketHandler? OnOutboundPacket;
    
    /// <summary>Raised when an exception occurs.</summary>
    public event ExceptionErrorHandler? OnExceptionError;
    
    /// <summary>Raised when starting fails.</summary>
    public event FailedHandler? OnFailed;
    #endregion

    /// <summary>
    /// Initializes a new instance of the MCComm class.
    /// </summary>
    public MCComm()
    {
        RegisterPackets();
    }

    private void RegisterPackets()
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

    /// <summary>
    /// Starts the HTTP server on the specified port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="loginKey">The login key for authentication.</param>
    public async Task StartAsync(ushort port, string loginKey)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        
        try
        {
            LoginKey = loginKey;
            _cts = new CancellationTokenSource();
            _webServer.Prefixes.Add($"http://*:{port}/");
            _webServer.Start();
            
            OnLoginReceived += HandleLoginReceived;
            OnLogoutReceived += HandleLogoutReceived;
            
            _activityChecker = Task.Run(ActivityCheckAsync);
            OnStarted?.Invoke();
            
            await ListenAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            OnFailed?.Invoke(ex);
        }
    }

    /// <summary>
    /// Stops the HTTP server and cleans up resources.
    /// </summary>
    /// <param name="reason">Optional reason for stopping.</param>
    public void Stop(string? reason = null)
    {
        if (!_webServer.IsListening) 
            return;
            
        _cts?.Cancel();
        _webServer.Stop();
        
        OnLoginReceived -= HandleLoginReceived;
        OnLogoutReceived -= HandleLogoutReceived;
        
        Sessions.Clear();
        _activityChecker = null;
        
        OnStopped?.Invoke(reason);
    }

    /// <summary>
    /// Sends a response packet to the client.
    /// </summary>
    /// <param name="ctx">The HTTP listener context.</param>
    /// <param name="code">The HTTP status code.</param>
    /// <param name="packet">The packet to send.</param>
    public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, MCCommPacket packet)
    {
        if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
            OnOutboundPacket?.Invoke(packet);

        var content = Encoding.UTF8.GetBytes(packet.SerializePacket());
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";
        ctx.Response.OutputStream.Write(content, 0, content.Length);
        ctx.Response.OutputStream.Close();
    }

    private async Task ListenAsync()
    {
        while (_webServer.IsListening)
        {
            try
            {
                var ctx = await _webServer.GetContextAsync().ConfigureAwait(false);
                
                // Offload processing to prevent blocking the listener
                _ = Task.Run(() => ProcessRequestAsync(ctx));
            }
            catch (HttpListenerException)
            {
                return; // Server stopped
            }
            catch (ObjectDisposedException)
            {
                return; // Server disposed
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.HttpMethod != HttpMethod.Post.Method)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                ctx.Response.Close();
                return;
            }

            // Security: Limit request size to prevent DoS
            if (ctx.Request.ContentLength64 > MaxRequestSize)
            {
                SendResponse(ctx, HttpStatusCode.RequestEntityTooLarge, new Deny { Reason = "Request too large" });
                return;
            }

            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Empty body" });
                return;
            }

            var packet = PacketRegistry.GetPacketFromJsonString(content);

            if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                OnInboundPacket?.Invoke(packet);

            HandlePacket(packet, ctx);
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                OnExceptionError?.Invoke(ex);

            try
            {
                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Invalid Data!" });
            }
            catch
            {
                // Context might be closed already
            }
        }
    }

    private void HandlePacket(MCCommPacket packet, HttpListenerContext ctx)
    {
        try
        {
            // Validate session for non-login packets
            if (packet.PacketId != (byte)MCCommPacketTypes.Login)
            {
                if (Sessions.TryGetValue(packet.Token, out var session))
                {
                    Sessions.TryUpdate(packet.Token, Environment.TickCount64, session);
                }
                else
                {
                    SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Token!" });
                    return;
                }
            }

            DispatchPacket(packet, ctx);
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                OnExceptionError?.Invoke(ex);

            SendResponse(ctx, HttpStatusCode.BadRequest, new Deny { Reason = "Invalid Data!" });
        }
    }

    private void DispatchPacket(MCCommPacket packet, HttpListenerContext ctx)
    {
        switch ((MCCommPacketTypes)packet.PacketId)
        {
            case MCCommPacketTypes.Login:
                OnLoginReceived?.Invoke((Login)packet, ctx);
                break;
            case MCCommPacketTypes.Logout:
                OnLogoutReceived?.Invoke((Logout)packet, ctx);
                break;
            case MCCommPacketTypes.Accept:
                OnAcceptReceived?.Invoke((Accept)packet, ctx);
                break;
            case MCCommPacketTypes.Deny:
                OnDenyReceived?.Invoke((Deny)packet, ctx);
                break;
            case MCCommPacketTypes.Bind:
                OnBindReceived?.Invoke((Bind)packet, ctx);
                break;
            case MCCommPacketTypes.Update:
                OnUpdateReceived?.Invoke((Update)packet, ctx);
                break;
            case MCCommPacketTypes.GetChannels:
                OnGetChannelsReceived?.Invoke((GetChannels)packet, ctx);
                break;
            case MCCommPacketTypes.AckUpdate:
                OnAckUpdateReceived?.Invoke((AckUpdate)packet, ctx);
                break;
            case MCCommPacketTypes.GetChannelSettings:
                OnGetChannelSettingsReceived?.Invoke((GetChannelSettings)packet, ctx);
                break;
            case MCCommPacketTypes.SetChannelSettings:
                OnSetChannelSettingsReceived?.Invoke((SetChannelSettings)packet, ctx);
                break;
            case MCCommPacketTypes.GetDefaultSettings:
                OnGetDefaultSettingsReceived?.Invoke((GetDefaultSettings)packet, ctx);
                break;
            case MCCommPacketTypes.SetDefaultSettings:
                OnSetDefaultSettingsReceived?.Invoke((SetDefaultSettings)packet, ctx);
                break;
            case MCCommPacketTypes.GetParticipants:
                OnGetParticipantsReceived?.Invoke((GetParticipants)packet, ctx);
                break;
            case MCCommPacketTypes.DisconnectParticipant:
                OnDisconnectParticipantReceived?.Invoke((DisconnectParticipant)packet, ctx);
                break;
            case MCCommPacketTypes.GetParticipantBitmask:
                OnGetParticipantBitmaskReceived?.Invoke((GetParticipantBitmask)packet, ctx);
                break;
            case MCCommPacketTypes.SetParticipantBitmask:
                OnSetParticipantBitmaskReceived?.Invoke((SetParticipantBitmask)packet, ctx);
                break;
            case MCCommPacketTypes.MuteParticipant:
                OnMuteParticipantReceived?.Invoke((MuteParticipant)packet, ctx);
                break;
            case MCCommPacketTypes.UnmuteParticipant:
                OnUnmuteParticipantReceived?.Invoke((UnmuteParticipant)packet, ctx);
                break;
            case MCCommPacketTypes.DeafenParticipant:
                OnDeafenParticipantReceived?.Invoke((DeafenParticipant)packet, ctx);
                break;
            case MCCommPacketTypes.UndeafenParticipant:
                OnUndeafenParticipantReceived?.Invoke((UndeafenParticipant)packet, ctx);
                break;
            case MCCommPacketTypes.ANDModParticipantBitmask:
                OnANDModParticipantBitmaskReceived?.Invoke((ANDModParticipantBitmask)packet, ctx);
                break;
            case MCCommPacketTypes.ORModParticipantBitmask:
                OnORModParticipantBitmaskReceived?.Invoke((ORModParticipantBitmask)packet, ctx);
                break;
            case MCCommPacketTypes.XORModParticipantBitmask:
                OnXORModParticipantBitmaskReceived?.Invoke((XORModParticipantBitmask)packet, ctx);
                break;
            case MCCommPacketTypes.ChannelMove:
                OnChannelMoveReceived?.Invoke((ChannelMove)packet, ctx);
                break;
        }
    }

    private void HandleLoginReceived(Login packet, HttpListenerContext ctx)
    {
        if (packet.LoginKey != LoginKey)
        {
            SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Login Key!" });
            return;
        }
        
        if (packet.Version != Version)
        {
            SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = $"Version mismatch! Expected: {Version}, Got: {packet.Version}" });
            return;
        }
        
        var token = Guid.NewGuid().ToString();
        Sessions.TryAdd(token, Environment.TickCount64);
        SendResponse(ctx, HttpStatusCode.OK, new Accept { Token = token });
        OnServerConnected?.Invoke(token, ctx.Request.RemoteEndPoint.Address.ToString());
    }

    private void HandleLogoutReceived(Logout packet, HttpListenerContext ctx)
    {
        if (Sessions.TryRemove(packet.Token, out _))
        {
            SendResponse(ctx, HttpStatusCode.OK, new Accept());
            OnServerDisconnected?.Invoke("Server disconnected gracefully", packet.Token);
            return;
        }
        
        SendResponse(ctx, HttpStatusCode.OK, new Deny { Reason = "Invalid Token!" });
    }

    private async Task ActivityCheckAsync()
    {
        while (_webServer.IsListening && _cts is { IsCancellationRequested: false })
        {
            foreach (var session in Sessions)
            {
                var elapsed = Environment.TickCount64 - session.Value;
                if (elapsed > Timeout && Sessions.TryRemove(session))
                {
                    OnServerDisconnected?.Invoke($"Server timed out after {elapsed}ms", session.Key);
                }
            }

            try
            {
                await Task.Delay(1000, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_webServer.IsListening)
                Stop("Disposed");
                
            _cts?.Dispose();
            _webServer.Close();
        }
    }
}

